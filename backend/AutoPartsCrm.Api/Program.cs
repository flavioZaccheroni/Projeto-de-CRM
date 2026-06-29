using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' nao configurada.");

builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new
{
    name = "CRM Autopecas e Servicos API",
    status = "online",
    version = "0.2.0"
}))
.WithName("ApiInfo");

app.MapGet("/health", async (NpgsqlDataSource db) =>
{
    await using var command = db.CreateCommand("select 1");
    var result = await command.ExecuteScalarAsync();

    return Results.Ok(new
    {
        status = result is 1 ? "healthy" : "degraded",
        database = "online",
        checkedAt = DateTimeOffset.UtcNow
    });
})
.WithName("HealthCheck");

app.MapPost("/api/auth/login", async (LoginRequest request, NpgsqlDataSource db) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Email e senha sao obrigatorios." });
    }

    const string sql = """
        select u.id, u.full_name, u.email, u.password_hash, u.is_active, r.name as role_name
        from users u
        join roles r on r.id = u.role_id
        where lower(u.email) = lower(@email)
        limit 1
        """;

    await using var command = db.CreateCommand(sql);
    command.Parameters.AddWithValue("email", request.Email.Trim());

    await using var reader = await command.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
    {
        return Results.Unauthorized();
    }

    var passwordHash = reader.GetString(3);
    var isActive = reader.GetBoolean(4);

    if (!isActive || !VerifyDevelopmentPassword(passwordHash, request.Password))
    {
        return Results.Unauthorized();
    }

    var userId = reader.GetGuid(0);
    var permissions = await LoadPermissions(db, userId);

    return Results.Ok(new LoginResponse(
        "dev-session",
        new CurrentUserDto(userId, reader.GetString(1), reader.GetString(2), reader.GetString(5), permissions)
    ));
});

app.MapGet("/api/dashboard", async (NpgsqlDataSource db) =>
{
    var customers = await Count(db, "customers");
    var products = await Count(db, "products");
    var services = await Count(db, "services");
    var openOrders = await Count(db, "work_orders", "status <> 'completed' and status <> 'cancelled'");
    var users = await Count(db, "users");

    return Results.Ok(new DashboardDto(customers, products, services, openOrders, users));
});

app.MapGet("/api/roles", async (NpgsqlDataSource db) =>
{
    const string sql = "select id, name, coalesce(description, '') from roles order by name";
    return Results.Ok(await Query(db, sql, reader => new RoleDto(reader.GetGuid(0), reader.GetString(1), reader.GetString(2))));
});

app.MapGet("/api/permissions", async (NpgsqlDataSource db) =>
{
    const string sql = "select id, code, description from permissions order by code";
    return Results.Ok(await Query(db, sql, reader => new PermissionDto(reader.GetGuid(0), reader.GetString(1), reader.GetString(2))));
});

app.MapGet("/api/users", async (NpgsqlDataSource db) =>
{
    const string sql = """
        select u.id, u.full_name, u.email, u.is_active, r.id, r.name
        from users u
        join roles r on r.id = u.role_id
        order by u.full_name
        """;

    return Results.Ok(await Query(db, sql, reader => new UserDto(
        reader.GetGuid(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetBoolean(3),
        reader.GetGuid(4),
        reader.GetString(5)
    )));
});

app.MapPost("/api/users", async (CreateUserRequest request, NpgsqlDataSource db) =>
{
    if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest(new { message = "Nome e email sao obrigatorios." });
    }

    const string sql = """
        insert into users (role_id, full_name, email, password_hash)
        values (@roleId, @fullName, @email, @passwordHash)
        returning id
        """;

    await using var command = db.CreateCommand(sql);
    command.Parameters.AddWithValue("roleId", request.RoleId);
    command.Parameters.AddWithValue("fullName", request.FullName.Trim());
    command.Parameters.AddWithValue("email", request.Email.Trim());
    command.Parameters.AddWithValue("passwordHash", $"dev:{(string.IsNullOrWhiteSpace(request.Password) ? "123456" : request.Password)}");

    var id = (Guid)(await command.ExecuteScalarAsync() ?? Guid.Empty);
    await InsertAudit(db, null, "users", id, "created", request);

    return Results.Created($"/api/users/{id}", new { id });
});

app.MapGet("/api/customers", async (NpgsqlDataSource db) =>
{
    const string sql = """
        select id, name, coalesce(document_number, ''), coalesce(phone, ''), coalesce(email, ''),
               coalesce(city, ''), coalesce(state, ''), is_active
        from customers
        order by name
        """;

    return Results.Ok(await Query(db, sql, reader => new CustomerDto(
        reader.GetGuid(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetString(4),
        reader.GetString(5),
        reader.GetString(6),
        reader.GetBoolean(7)
    )));
});

app.MapPost("/api/customers", async (CreateCustomerRequest request, NpgsqlDataSource db) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { message = "Nome do cliente e obrigatorio." });
    }

    const string sql = """
        insert into customers (name, document_number, phone, email, address_line, city, state, notes)
        values (@name, @document, @phone, @email, @address, @city, @state, @notes)
        returning id
        """;

    await using var command = db.CreateCommand(sql);
    command.Parameters.AddWithValue("name", request.Name.Trim());
    command.Parameters.AddWithValue("document", DbValue(request.DocumentNumber));
    command.Parameters.AddWithValue("phone", DbValue(request.Phone));
    command.Parameters.AddWithValue("email", DbValue(request.Email));
    command.Parameters.AddWithValue("address", DbValue(request.AddressLine));
    command.Parameters.AddWithValue("city", DbValue(request.City));
    command.Parameters.AddWithValue("state", DbValue(request.State));
    command.Parameters.AddWithValue("notes", DbValue(request.Notes));

    var id = (Guid)(await command.ExecuteScalarAsync() ?? Guid.Empty);
    await InsertAudit(db, request.UserId, "customers", id, "created", request);

    return Results.Created($"/api/customers/{id}", new { id });
});

app.MapGet("/api/vehicles", async (NpgsqlDataSource db) =>
{
    const string sql = """
        select v.id, v.customer_id, c.name, coalesce(v.plate, ''), coalesce(v.brand, ''),
               coalesce(v.model, ''), v.model_year, coalesce(v.color, '')
        from vehicles v
        join customers c on c.id = v.customer_id
        order by c.name, v.plate
        """;

    return Results.Ok(await Query(db, sql, reader => new VehicleDto(
        reader.GetGuid(0),
        reader.GetGuid(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetString(4),
        reader.GetString(5),
        reader.IsDBNull(6) ? null : reader.GetInt32(6),
        reader.GetString(7)
    )));
});

app.MapPost("/api/vehicles", async (CreateVehicleRequest request, NpgsqlDataSource db) =>
{
    const string sql = """
        insert into vehicles (customer_id, plate, brand, model, model_year, color, vin, notes)
        values (@customerId, @plate, @brand, @model, @year, @color, @vin, @notes)
        returning id
        """;

    await using var command = db.CreateCommand(sql);
    command.Parameters.AddWithValue("customerId", request.CustomerId);
    command.Parameters.AddWithValue("plate", DbValue(request.Plate));
    command.Parameters.AddWithValue("brand", DbValue(request.Brand));
    command.Parameters.AddWithValue("model", DbValue(request.Model));
    command.Parameters.AddWithValue("year", request.ModelYear is null ? DBNull.Value : request.ModelYear);
    command.Parameters.AddWithValue("color", DbValue(request.Color));
    command.Parameters.AddWithValue("vin", DbValue(request.Vin));
    command.Parameters.AddWithValue("notes", DbValue(request.Notes));

    var id = (Guid)(await command.ExecuteScalarAsync() ?? Guid.Empty);
    await InsertAudit(db, request.UserId, "vehicles", id, "created", request);

    return Results.Created($"/api/vehicles/{id}", new { id });
});

app.MapGet("/api/products", async (NpgsqlDataSource db) =>
{
    const string sql = """
        select p.id, p.sku, p.name, p.unit, p.sale_price, p.cost_price, p.minimum_stock,
               coalesce(s.quantity, 0), p.is_active
        from products p
        left join stock_balances s on s.product_id = p.id
        order by p.name
        """;

    return Results.Ok(await Query(db, sql, reader => new ProductDto(
        reader.GetGuid(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetDecimal(4),
        reader.GetDecimal(5),
        reader.GetDecimal(6),
        reader.GetDecimal(7),
        reader.GetBoolean(8)
    )));
});

app.MapPost("/api/products", async (CreateProductRequest request, NpgsqlDataSource db) =>
{
    if (string.IsNullOrWhiteSpace(request.Sku) || string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { message = "Codigo e nome do produto sao obrigatorios." });
    }

    await using var connection = await db.OpenConnectionAsync();
    await using var transaction = await connection.BeginTransactionAsync();

    await using var command = new NpgsqlCommand("""
        insert into products (sku, name, description, unit, sale_price, cost_price, minimum_stock)
        values (@sku, @name, @description, @unit, @salePrice, @costPrice, @minimumStock)
        returning id
        """, connection, transaction);

    command.Parameters.AddWithValue("sku", request.Sku.Trim());
    command.Parameters.AddWithValue("name", request.Name.Trim());
    command.Parameters.AddWithValue("description", DbValue(request.Description));
    command.Parameters.AddWithValue("unit", string.IsNullOrWhiteSpace(request.Unit) ? "UN" : request.Unit.Trim());
    command.Parameters.AddWithValue("salePrice", request.SalePrice);
    command.Parameters.AddWithValue("costPrice", request.CostPrice);
    command.Parameters.AddWithValue("minimumStock", request.MinimumStock);

    var id = (Guid)(await command.ExecuteScalarAsync() ?? Guid.Empty);

    await using var balance = new NpgsqlCommand("insert into stock_balances (product_id, quantity) values (@id, @quantity)", connection, transaction);
    balance.Parameters.AddWithValue("id", id);
    balance.Parameters.AddWithValue("quantity", request.InitialStock);
    await balance.ExecuteNonQueryAsync();

    await transaction.CommitAsync();
    await InsertAudit(db, request.UserId, "products", id, "created", request);

    return Results.Created($"/api/products/{id}", new { id });
});

app.MapGet("/api/services", async (NpgsqlDataSource db) =>
{
    const string sql = """
        select id, code, name, coalesce(description, ''), standard_price, estimated_minutes, is_active
        from services
        order by name
        """;

    return Results.Ok(await Query(db, sql, reader => new ServiceDto(
        reader.GetGuid(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetDecimal(4),
        reader.IsDBNull(5) ? null : reader.GetInt32(5),
        reader.GetBoolean(6)
    )));
});

app.MapPost("/api/services", async (CreateServiceRequest request, NpgsqlDataSource db) =>
{
    if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { message = "Codigo e nome do servico sao obrigatorios." });
    }

    const string sql = """
        insert into services (code, name, description, standard_price, estimated_minutes)
        values (@code, @name, @description, @price, @minutes)
        returning id
        """;

    await using var command = db.CreateCommand(sql);
    command.Parameters.AddWithValue("code", request.Code.Trim());
    command.Parameters.AddWithValue("name", request.Name.Trim());
    command.Parameters.AddWithValue("description", DbValue(request.Description));
    command.Parameters.AddWithValue("price", request.StandardPrice);
    command.Parameters.AddWithValue("minutes", request.EstimatedMinutes is null ? DBNull.Value : request.EstimatedMinutes);

    var id = (Guid)(await command.ExecuteScalarAsync() ?? Guid.Empty);
    await InsertAudit(db, request.UserId, "services", id, "created", request);

    return Results.Created($"/api/services/{id}", new { id });
});

app.MapGet("/api/audit-logs", async (NpgsqlDataSource db) =>
{
    const string sql = """
        select a.id, coalesce(u.full_name, 'Sistema'), a.entity_name, a.entity_id, a.action, a.created_at
        from audit_logs a
        left join users u on u.id = a.user_id
        order by a.created_at desc
        limit 100
        """;

    return Results.Ok(await Query(db, sql, reader => new AuditLogDto(
        reader.GetGuid(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.IsDBNull(3) ? null : reader.GetGuid(3),
        reader.GetString(4),
        reader.GetFieldValue<DateTimeOffset>(5)
    )));
});

app.Run();

static async Task<List<T>> Query<T>(NpgsqlDataSource db, string sql, Func<NpgsqlDataReader, T> map)
{
    var items = new List<T>();
    await using var command = db.CreateCommand(sql);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        items.Add(map(reader));
    }

    return items;
}

static async Task<int> Count(NpgsqlDataSource db, string table, string? where = null)
{
    var sql = $"select count(*) from {table}";
    if (!string.IsNullOrWhiteSpace(where))
    {
        sql += $" where {where}";
    }

    await using var command = db.CreateCommand(sql);
    return Convert.ToInt32(await command.ExecuteScalarAsync());
}

static async Task<List<string>> LoadPermissions(NpgsqlDataSource db, Guid userId)
{
    const string sql = """
        select p.code
        from users u
        join role_permissions rp on rp.role_id = u.role_id
        join permissions p on p.id = rp.permission_id
        where u.id = @userId
        order by p.code
        """;

    var permissions = new List<string>();
    await using var command = db.CreateCommand(sql);
    command.Parameters.AddWithValue("userId", userId);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        permissions.Add(reader.GetString(0));
    }

    return permissions;
}

static async Task InsertAudit(NpgsqlDataSource db, Guid? userId, string entityName, Guid entityId, string action, object newData)
{
    const string sql = """
        insert into audit_logs (user_id, entity_name, entity_id, action, new_data)
        values (@userId, @entityName, @entityId, @action, @newData::jsonb)
        """;

    await using var command = db.CreateCommand(sql);
    command.Parameters.AddWithValue("userId", userId is null ? DBNull.Value : userId);
    command.Parameters.AddWithValue("entityName", entityName);
    command.Parameters.AddWithValue("entityId", entityId);
    command.Parameters.AddWithValue("action", action);
    command.Parameters.AddWithValue("newData", System.Text.Json.JsonSerializer.Serialize(newData));
    await command.ExecuteNonQueryAsync();
}

static object DbValue(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

static bool VerifyDevelopmentPassword(string storedHash, string password)
{
    if (storedHash.StartsWith("dev:", StringComparison.Ordinal))
    {
        return storedHash[4..] == password;
    }

    return storedHash == "trocar-por-hash-real" && password == "123456";
}

record LoginRequest(string Email, string Password);
record LoginResponse(string Token, CurrentUserDto User);
record CurrentUserDto(Guid Id, string FullName, string Email, string RoleName, List<string> Permissions);
record DashboardDto(int Customers, int Products, int Services, int OpenOrders, int Users);
record RoleDto(Guid Id, string Name, string Description);
record PermissionDto(Guid Id, string Code, string Description);
record UserDto(Guid Id, string FullName, string Email, bool IsActive, Guid RoleId, string RoleName);
record CreateUserRequest(Guid RoleId, string FullName, string Email, string? Password);
record CustomerDto(Guid Id, string Name, string DocumentNumber, string Phone, string Email, string City, string State, bool IsActive);
record CreateCustomerRequest(string Name, string? DocumentNumber, string? Phone, string? Email, string? AddressLine, string? City, string? State, string? Notes, Guid? UserId);
record VehicleDto(Guid Id, Guid CustomerId, string CustomerName, string Plate, string Brand, string Model, int? ModelYear, string Color);
record CreateVehicleRequest(Guid CustomerId, string? Plate, string? Brand, string? Model, int? ModelYear, string? Color, string? Vin, string? Notes, Guid? UserId);
record ProductDto(Guid Id, string Sku, string Name, string Unit, decimal SalePrice, decimal CostPrice, decimal MinimumStock, decimal Quantity, bool IsActive);
record CreateProductRequest(string Sku, string Name, string? Description, string? Unit, decimal SalePrice, decimal CostPrice, decimal MinimumStock, decimal InitialStock, Guid? UserId);
record ServiceDto(Guid Id, string Code, string Name, string Description, decimal StandardPrice, int? EstimatedMinutes, bool IsActive);
record CreateServiceRequest(string Code, string Name, string? Description, decimal StandardPrice, int? EstimatedMinutes, Guid? UserId);
record AuditLogDto(Guid Id, string UserName, string EntityName, Guid? EntityId, string Action, DateTimeOffset CreatedAt);

public partial class Program;
