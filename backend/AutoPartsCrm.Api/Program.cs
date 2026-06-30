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
    var quotations = await Count(db, "quotations");
    var interactions = await Count(db, "customer_interactions");
    var salesOrders = await Count(db, "sales_orders");
    var purchaseOrders = await Count(db, "purchase_orders");
    var lowStock = await Count(db, "products p join stock_balances s on s.product_id = p.id", "s.quantity <= p.minimum_stock");

    return Results.Ok(new DashboardDto(customers, products, services, openOrders, users, quotations, interactions, salesOrders, purchaseOrders, lowStock));
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

app.MapGet("/api/stock-balances", async (NpgsqlDataSource db) =>
{
    const string sql = """
        select p.id, p.sku, p.name, p.unit, p.minimum_stock, coalesce(s.quantity, 0),
               case when coalesce(s.quantity, 0) <= p.minimum_stock then true else false end as below_minimum
        from products p
        left join stock_balances s on s.product_id = p.id
        where p.is_active = true
        order by p.name
        """;

    return Results.Ok(await Query(db, sql, reader => new StockBalanceDto(
        reader.GetGuid(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetDecimal(4),
        reader.GetDecimal(5),
        reader.GetBoolean(6)
    )));
});

app.MapGet("/api/stock-movements", async (NpgsqlDataSource db) =>
{
    const string sql = """
        select m.id, m.product_id, p.sku, p.name, coalesce(u.full_name, 'Sistema'),
               m.movement_type, m.quantity, m.reason, m.created_at
        from stock_movements m
        join products p on p.id = m.product_id
        left join users u on u.id = m.user_id
        order by m.created_at desc
        limit 150
        """;

    return Results.Ok(await Query(db, sql, reader => new StockMovementDto(
        reader.GetGuid(0),
        reader.GetGuid(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetString(4),
        reader.GetString(5),
        reader.GetDecimal(6),
        reader.GetString(7),
        reader.GetFieldValue<DateTimeOffset>(8)
    )));
});

app.MapPost("/api/stock-movements", async (CreateStockMovementRequest request, NpgsqlDataSource db) =>
{
    if (request.Quantity <= 0)
    {
        return Results.BadRequest(new { message = "Quantidade deve ser maior que zero." });
    }

    await using var connection = await db.OpenConnectionAsync();
    await using var transaction = await connection.BeginTransactionAsync();

    var current = await GetStockQuantity(connection, transaction, request.ProductId);
    var next = request.MovementType switch
    {
        "in" => current + request.Quantity,
        "out" => current - request.Quantity,
        "adjustment" => request.Quantity,
        _ => throw new InvalidOperationException("Tipo de movimento invalido.")
    };

    if (next < 0)
    {
        return Results.BadRequest(new { message = "Estoque nao pode ficar negativo." });
    }

    await UpsertStockBalance(connection, transaction, request.ProductId, next);
    await InsertStockMovement(connection, transaction, request.ProductId, null, request.UserId, request.MovementType, request.Quantity, request.Reason);
    await transaction.CommitAsync();
    await InsertAudit(db, request.UserId, "stock_movements", request.ProductId, "created", request);

    return Results.Created("/api/stock-movements", new { productId = request.ProductId, quantity = next });
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

app.MapGet("/api/customer-interactions", async (NpgsqlDataSource db) =>
{
    const string sql = """
        select i.id, i.customer_id, c.name, coalesce(u.full_name, 'Sistema'),
               i.interaction_type, i.subject, coalesce(i.description, ''),
               i.status, i.next_contact_at, i.created_at
        from customer_interactions i
        join customers c on c.id = i.customer_id
        left join users u on u.id = i.user_id
        order by i.created_at desc
        """;

    return Results.Ok(await Query(db, sql, reader => new CustomerInteractionDto(
        reader.GetGuid(0),
        reader.GetGuid(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetString(4),
        reader.GetString(5),
        reader.GetString(6),
        reader.GetString(7),
        reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
        reader.GetFieldValue<DateTimeOffset>(9)
    )));
});

app.MapPost("/api/customer-interactions", async (CreateCustomerInteractionRequest request, NpgsqlDataSource db) =>
{
    if (string.IsNullOrWhiteSpace(request.Subject))
    {
        return Results.BadRequest(new { message = "Assunto do atendimento e obrigatorio." });
    }

    const string sql = """
        insert into customer_interactions (customer_id, user_id, interaction_type, subject, description, status, next_contact_at)
        values (@customerId, @userId, @type, @subject, @description, @status, @nextContactAt)
        returning id
        """;

    await using var command = db.CreateCommand(sql);
    command.Parameters.AddWithValue("customerId", request.CustomerId);
    command.Parameters.AddWithValue("userId", request.UserId is null ? DBNull.Value : request.UserId);
    command.Parameters.AddWithValue("type", string.IsNullOrWhiteSpace(request.InteractionType) ? "other" : request.InteractionType.Trim());
    command.Parameters.AddWithValue("subject", request.Subject.Trim());
    command.Parameters.AddWithValue("description", DbValue(request.Description));
    command.Parameters.AddWithValue("status", string.IsNullOrWhiteSpace(request.Status) ? "open" : request.Status.Trim());
    command.Parameters.AddWithValue("nextContactAt", request.NextContactAt is null ? DBNull.Value : request.NextContactAt);

    var id = (Guid)(await command.ExecuteScalarAsync() ?? Guid.Empty);
    await InsertAudit(db, request.UserId, "customer_interactions", id, "created", request);

    return Results.Created($"/api/customer-interactions/{id}", new { id });
});

app.MapGet("/api/quotations", async (NpgsqlDataSource db) =>
{
    const string sql = """
        select q.id, q.customer_id, c.name, q.vehicle_id, coalesce(v.plate, ''),
               q.status, coalesce(q.notes, ''), q.total_products, q.total_services,
               q.total_amount, q.created_at, count(qi.id)::int
        from quotations q
        join customers c on c.id = q.customer_id
        left join vehicles v on v.id = q.vehicle_id
        left join quotation_items qi on qi.quotation_id = q.id
        group by q.id, c.name, v.plate
        order by q.created_at desc
        """;

    return Results.Ok(await Query(db, sql, reader => new QuotationDto(
        reader.GetGuid(0),
        reader.GetGuid(1),
        reader.GetString(2),
        reader.IsDBNull(3) ? null : reader.GetGuid(3),
        reader.GetString(4),
        reader.GetString(5),
        reader.GetString(6),
        reader.GetDecimal(7),
        reader.GetDecimal(8),
        reader.GetDecimal(9),
        reader.GetFieldValue<DateTimeOffset>(10),
        reader.GetInt32(11)
    )));
});

app.MapPost("/api/quotations", async (CreateQuotationRequest request, NpgsqlDataSource db) =>
{
    if (request.Items.Count == 0)
    {
        return Results.BadRequest(new { message = "O orcamento precisa ter pelo menos um item." });
    }

    var totals = CalculateTotals(request.Items);
    await using var connection = await db.OpenConnectionAsync();
    await using var transaction = await connection.BeginTransactionAsync();

    await using var command = new NpgsqlCommand("""
        insert into quotations (customer_id, vehicle_id, created_by_user_id, status, notes, total_products, total_services, total_amount)
        values (@customerId, @vehicleId, @userId, 'draft', @notes, @totalProducts, @totalServices, @totalAmount)
        returning id
        """, connection, transaction);
    command.Parameters.AddWithValue("customerId", request.CustomerId);
    command.Parameters.AddWithValue("vehicleId", request.VehicleId is null ? DBNull.Value : request.VehicleId);
    command.Parameters.AddWithValue("userId", request.UserId ?? throw new InvalidOperationException("Usuario obrigatorio."));
    command.Parameters.AddWithValue("notes", DbValue(request.Notes));
    command.Parameters.AddWithValue("totalProducts", totals.Products);
    command.Parameters.AddWithValue("totalServices", totals.Services);
    command.Parameters.AddWithValue("totalAmount", totals.Amount);

    var id = (Guid)(await command.ExecuteScalarAsync() ?? Guid.Empty);

    foreach (var item in request.Items)
    {
        await InsertCommercialItem(connection, transaction, "quotation_items", "quotation_id", id, item);
    }

    await transaction.CommitAsync();
    await InsertAudit(db, request.UserId, "quotations", id, "created", request);

    return Results.Created($"/api/quotations/{id}", new { id });
});

app.MapPost("/api/quotations/{id:guid}/status", async (Guid id, UpdateStatusRequest request, NpgsqlDataSource db) =>
{
    const string sql = "update quotations set status = @status, updated_at = now() where id = @id";
    await using var command = db.CreateCommand(sql);
    command.Parameters.AddWithValue("id", id);
    command.Parameters.AddWithValue("status", request.Status.Trim());
    var rows = await command.ExecuteNonQueryAsync();

    if (rows == 0)
    {
        return Results.NotFound();
    }

    await InsertAudit(db, request.UserId, "quotations", id, "status_updated", request);
    return Results.Ok(new { id, request.Status });
});

app.MapGet("/api/sales-orders", async (NpgsqlDataSource db) =>
{
    const string sql = """
        select s.id, s.customer_id, c.name, s.quotation_id, s.status, coalesce(s.notes, ''),
               s.total_products, s.total_services, s.total_amount, s.created_at, count(si.id)::int
        from sales_orders s
        join customers c on c.id = s.customer_id
        left join sales_order_items si on si.sales_order_id = s.id
        group by s.id, c.name
        order by s.created_at desc
        """;

    return Results.Ok(await Query(db, sql, reader => new SalesOrderDto(
        reader.GetGuid(0),
        reader.GetGuid(1),
        reader.GetString(2),
        reader.IsDBNull(3) ? null : reader.GetGuid(3),
        reader.GetString(4),
        reader.GetString(5),
        reader.GetDecimal(6),
        reader.GetDecimal(7),
        reader.GetDecimal(8),
        reader.GetFieldValue<DateTimeOffset>(9),
        reader.GetInt32(10)
    )));
});

app.MapPost("/api/sales-orders", async (CreateSalesOrderRequest request, NpgsqlDataSource db) =>
{
    if (request.Items.Count == 0)
    {
        return Results.BadRequest(new { message = "O pedido precisa ter pelo menos um item." });
    }

    var totals = CalculateTotals(request.Items);
    await using var connection = await db.OpenConnectionAsync();
    await using var transaction = await connection.BeginTransactionAsync();

    await using var command = new NpgsqlCommand("""
        insert into sales_orders (customer_id, quotation_id, created_by_user_id, status, notes, total_products, total_services, total_amount)
        values (@customerId, @quotationId, @userId, 'draft', @notes, @totalProducts, @totalServices, @totalAmount)
        returning id
        """, connection, transaction);
    command.Parameters.AddWithValue("customerId", request.CustomerId);
    command.Parameters.AddWithValue("quotationId", request.QuotationId is null ? DBNull.Value : request.QuotationId);
    command.Parameters.AddWithValue("userId", request.UserId ?? throw new InvalidOperationException("Usuario obrigatorio."));
    command.Parameters.AddWithValue("notes", DbValue(request.Notes));
    command.Parameters.AddWithValue("totalProducts", totals.Products);
    command.Parameters.AddWithValue("totalServices", totals.Services);
    command.Parameters.AddWithValue("totalAmount", totals.Amount);

    var id = (Guid)(await command.ExecuteScalarAsync() ?? Guid.Empty);

    foreach (var item in request.Items)
    {
        await InsertCommercialItem(connection, transaction, "sales_order_items", "sales_order_id", id, item);
    }

    await transaction.CommitAsync();
    await InsertAudit(db, request.UserId, "sales_orders", id, "created", request);

    return Results.Created($"/api/sales-orders/{id}", new { id });
});

app.MapPost("/api/sales-orders/{id:guid}/status", async (Guid id, UpdateStatusRequest request, NpgsqlDataSource db) =>
{
    const string sql = "update sales_orders set status = @status, updated_at = now() where id = @id";
    await using var command = db.CreateCommand(sql);
    command.Parameters.AddWithValue("id", id);
    command.Parameters.AddWithValue("status", request.Status.Trim());
    var rows = await command.ExecuteNonQueryAsync();

    if (rows == 0)
    {
        return Results.NotFound();
    }

    await InsertAudit(db, request.UserId, "sales_orders", id, "status_updated", request);
    return Results.Ok(new { id, request.Status });
});

app.MapGet("/api/purchase-orders", async (NpgsqlDataSource db) =>
{
    const string sql = """
        select po.id, po.status, coalesce(po.notes, ''), coalesce(po.expected_at::text, ''), po.total_amount,
               po.created_at, coalesce(u.full_name, 'Sistema'), count(pi.id)::int
        from purchase_orders po
        join users u on u.id = po.created_by_user_id
        left join purchase_order_items pi on pi.purchase_order_id = po.id
        group by po.id, u.full_name
        order by po.created_at desc
        """;

    return Results.Ok(await Query(db, sql, reader => new PurchaseOrderDto(
        reader.GetGuid(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetDecimal(4),
        reader.GetFieldValue<DateTimeOffset>(5),
        reader.GetString(6),
        reader.GetInt32(7)
    )));
});

app.MapPost("/api/purchase-orders", async (CreatePurchaseOrderRequest request, NpgsqlDataSource db) =>
{
    if (request.Items.Count == 0)
    {
        return Results.BadRequest(new { message = "Pedido de compra precisa ter pelo menos um item." });
    }

    var total = request.Items.Sum(item => item.Quantity * item.UnitCost);
    await using var connection = await db.OpenConnectionAsync();
    await using var transaction = await connection.BeginTransactionAsync();

    await using var command = new NpgsqlCommand("""
        insert into purchase_orders (created_by_user_id, status, expected_at, notes, total_amount)
        values (@userId, 'draft', @expectedAt, @notes, @totalAmount)
        returning id
        """, connection, transaction);
    command.Parameters.AddWithValue("userId", request.UserId ?? throw new InvalidOperationException("Usuario obrigatorio."));
    command.Parameters.AddWithValue("expectedAt", request.ExpectedAt is null ? DBNull.Value : request.ExpectedAt);
    command.Parameters.AddWithValue("notes", DbValue(request.Notes));
    command.Parameters.AddWithValue("totalAmount", total);

    var id = (Guid)(await command.ExecuteScalarAsync() ?? Guid.Empty);

    foreach (var item in request.Items)
    {
        await InsertPurchaseOrderItem(connection, transaction, id, item);
    }

    await transaction.CommitAsync();
    await InsertAudit(db, request.UserId, "purchase_orders", id, "created", request);

    return Results.Created($"/api/purchase-orders/{id}", new { id });
});

app.MapPost("/api/purchase-orders/{id:guid}/receive", async (Guid id, ReceivePurchaseOrderRequest request, NpgsqlDataSource db) =>
{
    await using var connection = await db.OpenConnectionAsync();
    await using var transaction = await connection.BeginTransactionAsync();

    var items = await LoadPurchaseItems(connection, transaction, id);
    if (items.Count == 0)
    {
        return Results.NotFound();
    }

    await using var receiptCommand = new NpgsqlCommand("""
        insert into purchase_receipts (purchase_order_id, user_id, notes)
        values (@orderId, @userId, @notes)
        returning id
        """, connection, transaction);
    receiptCommand.Parameters.AddWithValue("orderId", id);
    receiptCommand.Parameters.AddWithValue("userId", request.UserId ?? throw new InvalidOperationException("Usuario obrigatorio."));
    receiptCommand.Parameters.AddWithValue("notes", DbValue(request.Notes));
    var receiptId = (Guid)(await receiptCommand.ExecuteScalarAsync() ?? Guid.Empty);

    foreach (var item in items)
    {
        var quantityToReceive = item.Quantity - item.ReceivedQuantity;
        if (quantityToReceive <= 0)
        {
            continue;
        }

        var current = await GetStockQuantity(connection, transaction, item.ProductId);
        await UpsertStockBalance(connection, transaction, item.ProductId, current + quantityToReceive);
        await InsertStockMovement(connection, transaction, item.ProductId, null, request.UserId, "in", quantityToReceive, $"Recebimento do pedido de compra {id}");

        await using var receiptItemCommand = new NpgsqlCommand("""
            insert into purchase_receipt_items (purchase_receipt_id, purchase_order_item_id, product_id, quantity)
            values (@receiptId, @itemId, @productId, @quantity)
            """, connection, transaction);
        receiptItemCommand.Parameters.AddWithValue("receiptId", receiptId);
        receiptItemCommand.Parameters.AddWithValue("itemId", item.Id);
        receiptItemCommand.Parameters.AddWithValue("productId", item.ProductId);
        receiptItemCommand.Parameters.AddWithValue("quantity", quantityToReceive);
        await receiptItemCommand.ExecuteNonQueryAsync();

        await using var updateItemCommand = new NpgsqlCommand("""
            update purchase_order_items
            set received_quantity = received_quantity + @quantity
            where id = @itemId
            """, connection, transaction);
        updateItemCommand.Parameters.AddWithValue("quantity", quantityToReceive);
        updateItemCommand.Parameters.AddWithValue("itemId", item.Id);
        await updateItemCommand.ExecuteNonQueryAsync();
    }

    await using var updateOrderCommand = new NpgsqlCommand("update purchase_orders set status = 'received', updated_at = now() where id = @id", connection, transaction);
    updateOrderCommand.Parameters.AddWithValue("id", id);
    await updateOrderCommand.ExecuteNonQueryAsync();

    await transaction.CommitAsync();
    await InsertAudit(db, request.UserId, "purchase_orders", id, "received", request);

    return Results.Ok(new { id, receiptId });
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

static CommercialTotals CalculateTotals(List<CommercialItemRequest> items)
{
    decimal products = 0;
    decimal services = 0;

    foreach (var item in items)
    {
        var total = item.Quantity * item.UnitPrice;
        if (item.ItemType == "product")
        {
            products += total;
        }
        else if (item.ItemType == "service")
        {
            services += total;
        }
    }

    return new CommercialTotals(products, services, products + services);
}

static async Task InsertCommercialItem(
    NpgsqlConnection connection,
    NpgsqlTransaction transaction,
    string tableName,
    string parentColumnName,
    Guid parentId,
    CommercialItemRequest item)
{
    if (item.Quantity <= 0)
    {
        throw new InvalidOperationException("Quantidade do item deve ser maior que zero.");
    }

    if (item.UnitPrice < 0)
    {
        throw new InvalidOperationException("Preco do item nao pode ser negativo.");
    }

    await using var command = new NpgsqlCommand($"""
        insert into {tableName} ({parentColumnName}, item_type, product_id, service_id, description, quantity, unit_price, total_amount)
        values (@parentId, @itemType, @productId, @serviceId, @description, @quantity, @unitPrice, @totalAmount)
        """, connection, transaction);

    command.Parameters.AddWithValue("parentId", parentId);
    command.Parameters.AddWithValue("itemType", item.ItemType);
    command.Parameters.AddWithValue("productId", item.ProductId is null ? DBNull.Value : item.ProductId);
    command.Parameters.AddWithValue("serviceId", item.ServiceId is null ? DBNull.Value : item.ServiceId);
    command.Parameters.AddWithValue("description", item.Description.Trim());
    command.Parameters.AddWithValue("quantity", item.Quantity);
    command.Parameters.AddWithValue("unitPrice", item.UnitPrice);
    command.Parameters.AddWithValue("totalAmount", item.Quantity * item.UnitPrice);
    await command.ExecuteNonQueryAsync();
}

static async Task<decimal> GetStockQuantity(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid productId)
{
    await using var command = new NpgsqlCommand("select coalesce(quantity, 0) from stock_balances where product_id = @productId", connection, transaction);
    command.Parameters.AddWithValue("productId", productId);
    return Convert.ToDecimal(await command.ExecuteScalarAsync() ?? 0);
}

static async Task UpsertStockBalance(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid productId, decimal quantity)
{
    await using var command = new NpgsqlCommand("""
        insert into stock_balances (product_id, quantity, updated_at)
        values (@productId, @quantity, now())
        on conflict (product_id) do update
        set quantity = excluded.quantity,
            updated_at = now()
        """, connection, transaction);
    command.Parameters.AddWithValue("productId", productId);
    command.Parameters.AddWithValue("quantity", quantity);
    await command.ExecuteNonQueryAsync();
}

static async Task InsertStockMovement(
    NpgsqlConnection connection,
    NpgsqlTransaction transaction,
    Guid productId,
    Guid? workOrderId,
    Guid? userId,
    string movementType,
    decimal quantity,
    string reason)
{
    await using var command = new NpgsqlCommand("""
        insert into stock_movements (product_id, work_order_id, user_id, movement_type, quantity, reason)
        values (@productId, @workOrderId, @userId, @movementType, @quantity, @reason)
        """, connection, transaction);
    command.Parameters.AddWithValue("productId", productId);
    command.Parameters.AddWithValue("workOrderId", workOrderId is null ? DBNull.Value : workOrderId);
    command.Parameters.AddWithValue("userId", userId ?? throw new InvalidOperationException("Usuario obrigatorio."));
    command.Parameters.AddWithValue("movementType", movementType);
    command.Parameters.AddWithValue("quantity", quantity);
    command.Parameters.AddWithValue("reason", reason.Trim());
    await command.ExecuteNonQueryAsync();
}

static async Task InsertPurchaseOrderItem(
    NpgsqlConnection connection,
    NpgsqlTransaction transaction,
    Guid purchaseOrderId,
    PurchaseOrderItemRequest item)
{
    await using var command = new NpgsqlCommand("""
        insert into purchase_order_items (purchase_order_id, product_id, description, quantity, unit_cost, total_amount)
        values (@orderId, @productId, @description, @quantity, @unitCost, @totalAmount)
        """, connection, transaction);
    command.Parameters.AddWithValue("orderId", purchaseOrderId);
    command.Parameters.AddWithValue("productId", item.ProductId);
    command.Parameters.AddWithValue("description", item.Description.Trim());
    command.Parameters.AddWithValue("quantity", item.Quantity);
    command.Parameters.AddWithValue("unitCost", item.UnitCost);
    command.Parameters.AddWithValue("totalAmount", item.Quantity * item.UnitCost);
    await command.ExecuteNonQueryAsync();
}

static async Task<List<PurchaseItemForReceipt>> LoadPurchaseItems(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid purchaseOrderId)
{
    var items = new List<PurchaseItemForReceipt>();
    await using var command = new NpgsqlCommand("""
        select id, product_id, quantity, received_quantity
        from purchase_order_items
        where purchase_order_id = @orderId
        """, connection, transaction);
    command.Parameters.AddWithValue("orderId", purchaseOrderId);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        items.Add(new PurchaseItemForReceipt(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetDecimal(2),
            reader.GetDecimal(3)
        ));
    }

    return items;
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
record DashboardDto(int Customers, int Products, int Services, int OpenOrders, int Users, int Quotations, int Interactions, int SalesOrders, int PurchaseOrders, int LowStock);
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
record StockBalanceDto(Guid ProductId, string Sku, string Name, string Unit, decimal MinimumStock, decimal Quantity, bool BelowMinimum);
record StockMovementDto(Guid Id, Guid ProductId, string Sku, string ProductName, string UserName, string MovementType, decimal Quantity, string Reason, DateTimeOffset CreatedAt);
record CreateStockMovementRequest(Guid ProductId, Guid? UserId, string MovementType, decimal Quantity, string Reason);
record ServiceDto(Guid Id, string Code, string Name, string Description, decimal StandardPrice, int? EstimatedMinutes, bool IsActive);
record CreateServiceRequest(string Code, string Name, string? Description, decimal StandardPrice, int? EstimatedMinutes, Guid? UserId);
record CustomerInteractionDto(Guid Id, Guid CustomerId, string CustomerName, string UserName, string InteractionType, string Subject, string Description, string Status, DateTimeOffset? NextContactAt, DateTimeOffset CreatedAt);
record CreateCustomerInteractionRequest(Guid CustomerId, Guid? UserId, string InteractionType, string Subject, string? Description, string Status, DateTimeOffset? NextContactAt);
record CommercialItemRequest(string ItemType, Guid? ProductId, Guid? ServiceId, string Description, decimal Quantity, decimal UnitPrice);
record CommercialTotals(decimal Products, decimal Services, decimal Amount);
record QuotationDto(Guid Id, Guid CustomerId, string CustomerName, Guid? VehicleId, string VehiclePlate, string Status, string Notes, decimal TotalProducts, decimal TotalServices, decimal TotalAmount, DateTimeOffset CreatedAt, int ItemsCount);
record CreateQuotationRequest(Guid CustomerId, Guid? VehicleId, Guid? UserId, string? Notes, List<CommercialItemRequest> Items);
record SalesOrderDto(Guid Id, Guid CustomerId, string CustomerName, Guid? QuotationId, string Status, string Notes, decimal TotalProducts, decimal TotalServices, decimal TotalAmount, DateTimeOffset CreatedAt, int ItemsCount);
record CreateSalesOrderRequest(Guid CustomerId, Guid? QuotationId, Guid? UserId, string? Notes, List<CommercialItemRequest> Items);
record UpdateStatusRequest(string Status, Guid? UserId);
record PurchaseOrderDto(Guid Id, string Status, string Notes, string ExpectedAt, decimal TotalAmount, DateTimeOffset CreatedAt, string UserName, int ItemsCount);
record PurchaseOrderItemRequest(Guid ProductId, string Description, decimal Quantity, decimal UnitCost);
record CreatePurchaseOrderRequest(Guid? UserId, DateTime? ExpectedAt, string? Notes, List<PurchaseOrderItemRequest> Items);
record ReceivePurchaseOrderRequest(Guid? UserId, string? Notes);
record PurchaseItemForReceipt(Guid Id, Guid ProductId, decimal Quantity, decimal ReceivedQuantity);
record AuditLogDto(Guid Id, string UserName, string EntityName, Guid? EntityId, string Action, DateTimeOffset CreatedAt);

public partial class Program;
