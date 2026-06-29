import 'dart:convert';
import 'dart:io';

import 'package:flutter/material.dart';

void main() {
  runApp(const AutoPartsCrmApp());
}

class AutoPartsCrmApp extends StatelessWidget {
  const AutoPartsCrmApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'CRM Autopecas e Servicos',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF2563EB),
          brightness: Brightness.light,
        ),
        useMaterial3: true,
      ),
      home: LoginPage(api: ApiClient()),
    );
  }
}

class ApiClient {
  ApiClient({this.baseUrl = 'http://localhost:5026'});

  final String baseUrl;
  final HttpClient _client = HttpClient()
    ..badCertificateCallback = (_, _, _) => true;

  Future<Map<String, dynamic>> getMap(String path) async {
    final data = await _request('GET', path);
    return data as Map<String, dynamic>;
  }

  Future<List<dynamic>> getList(String path) async {
    final data = await _request('GET', path);
    return data as List<dynamic>;
  }

  Future<Map<String, dynamic>> post(
    String path,
    Map<String, dynamic> body,
  ) async {
    final data = await _request('POST', path, body: body);
    return data as Map<String, dynamic>;
  }

  Future<dynamic> _request(
    String method,
    String path, {
    Map<String, dynamic>? body,
  }) async {
    final uri = Uri.parse('$baseUrl$path');
    final request = await _client.openUrl(method, uri);
    request.headers.contentType = ContentType.json;

    if (body != null) {
      request.write(jsonEncode(body));
    }

    final response = await request.close();
    final text = await response.transform(utf8.decoder).join();

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw ApiException(
        response.statusCode,
        text.isEmpty ? 'Falha na API.' : text,
      );
    }

    if (text.isEmpty) {
      return <String, dynamic>{};
    }

    return jsonDecode(text);
  }
}

class ApiException implements Exception {
  ApiException(this.statusCode, this.message);

  final int statusCode;
  final String message;

  @override
  String toString() => 'Erro $statusCode: $message';
}

class Session {
  const Session({
    required this.userId,
    required this.fullName,
    required this.email,
    required this.roleName,
    required this.permissions,
  });

  final String userId;
  final String fullName;
  final String email;
  final String roleName;
  final List<String> permissions;

  factory Session.fromJson(Map<String, dynamic> json) {
    final user = json['user'] as Map<String, dynamic>;
    return Session(
      userId: user['id'] as String,
      fullName: user['fullName'] as String,
      email: user['email'] as String,
      roleName: user['roleName'] as String,
      permissions: (user['permissions'] as List<dynamic>).cast<String>(),
    );
  }
}

class LoginPage extends StatefulWidget {
  const LoginPage({required this.api, super.key});

  final ApiClient api;

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final _email = TextEditingController(text: 'admin@crm.local');
  final _password = TextEditingController(text: '123456');
  bool _loading = false;
  String? _error;

  Future<void> _login() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final result = await widget.api.post('/api/auth/login', {
        'email': _email.text,
        'password': _password.text,
      });

      if (!mounted) return;
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) =>
              HomeShell(api: widget.api, session: Session.fromJson(result)),
        ),
      );
    } catch (error) {
      setState(
        () => _error = 'Nao foi possivel entrar. Verifique a API e os dados.',
      );
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 420),
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'CRM Autopecas e Servicos',
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
                const SizedBox(height: 8),
                Text(
                  'Acesso ao ambiente de desenvolvimento',
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
                const SizedBox(height: 24),
                TextField(
                  controller: _email,
                  decoration: const InputDecoration(
                    labelText: 'Email',
                    prefixIcon: Icon(Icons.mail_outline),
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: _password,
                  obscureText: true,
                  decoration: const InputDecoration(
                    labelText: 'Senha',
                    prefixIcon: Icon(Icons.lock_outline),
                  ),
                  onSubmitted: (_) => _login(),
                ),
                if (_error != null) ...[
                  const SizedBox(height: 12),
                  Text(
                    _error!,
                    style: TextStyle(
                      color: Theme.of(context).colorScheme.error,
                    ),
                  ),
                ],
                const SizedBox(height: 20),
                FilledButton.icon(
                  onPressed: _loading ? null : _login,
                  icon: _loading
                      ? const SizedBox(
                          width: 18,
                          height: 18,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Icon(Icons.login),
                  label: const Text('Entrar'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class HomeShell extends StatefulWidget {
  const HomeShell({required this.api, required this.session, super.key});

  final ApiClient api;
  final Session session;

  @override
  State<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends State<HomeShell> {
  int _index = 0;

  @override
  Widget build(BuildContext context) {
    final pages = [
      DashboardPage(api: widget.api),
      UsersPage(api: widget.api, session: widget.session),
      CustomersPage(api: widget.api, session: widget.session),
      VehiclesPage(api: widget.api, session: widget.session),
      ProductsPage(api: widget.api, session: widget.session),
      ServicesPage(api: widget.api, session: widget.session),
      AuditPage(api: widget.api),
    ];

    return Scaffold(
      body: Row(
        children: [
          NavigationRail(
            extended: true,
            selectedIndex: _index,
            onDestinationSelected: (value) => setState(() => _index = value),
            destinations: const [
              NavigationRailDestination(
                icon: Icon(Icons.dashboard_outlined),
                selectedIcon: Icon(Icons.dashboard),
                label: Text('Painel'),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.manage_accounts_outlined),
                selectedIcon: Icon(Icons.manage_accounts),
                label: Text('Usuarios'),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.people_outline),
                selectedIcon: Icon(Icons.people),
                label: Text('Clientes'),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.directions_car_outlined),
                selectedIcon: Icon(Icons.directions_car),
                label: Text('Veiculos'),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.inventory_2_outlined),
                selectedIcon: Icon(Icons.inventory_2),
                label: Text('Produtos'),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.build_outlined),
                selectedIcon: Icon(Icons.build),
                label: Text('Servicos'),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.history_outlined),
                selectedIcon: Icon(Icons.history),
                label: Text('Auditoria'),
              ),
            ],
          ),
          const VerticalDivider(width: 1),
          Expanded(
            child: Column(
              children: [
                AppBar(
                  title: const Text('CRM Autopecas e Servicos'),
                  actions: [
                    Padding(
                      padding: const EdgeInsets.only(right: 16),
                      child: Center(
                        child: Text(
                          '${widget.session.fullName} - ${widget.session.roleName}',
                        ),
                      ),
                    ),
                  ],
                ),
                Expanded(child: pages[_index]),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class DashboardPage extends StatelessWidget {
  const DashboardPage({required this.api, super.key});

  final ApiClient api;

  @override
  Widget build(BuildContext context) {
    return DataFuture(
      future: api.getMap('/api/dashboard'),
      builder: (context, data) => PageScaffold(
        title: 'Painel inicial',
        child: Wrap(
          spacing: 16,
          runSpacing: 16,
          children: [
            SummaryCard(
              title: 'Clientes',
              value: '${data['customers']}',
              icon: Icons.people,
            ),
            SummaryCard(
              title: 'Usuarios',
              value: '${data['users']}',
              icon: Icons.manage_accounts,
            ),
            SummaryCard(
              title: 'Produtos',
              value: '${data['products']}',
              icon: Icons.inventory_2,
            ),
            SummaryCard(
              title: 'Servicos',
              value: '${data['services']}',
              icon: Icons.build,
            ),
            SummaryCard(
              title: 'Ordens abertas',
              value: '${data['openOrders']}',
              icon: Icons.car_repair,
            ),
          ],
        ),
      ),
    );
  }
}

class UsersPage extends StatefulWidget {
  const UsersPage({required this.api, required this.session, super.key});

  final ApiClient api;
  final Session session;

  @override
  State<UsersPage> createState() => _UsersPageState();
}

class _UsersPageState extends State<UsersPage> {
  int _reload = 0;

  @override
  Widget build(BuildContext context) {
    return DataFuture(
      key: ValueKey(_reload),
      future: widget.api.getList('/api/users'),
      builder: (context, users) => PageScaffold(
        title: 'Usuarios',
        action: FilledButton.icon(
          onPressed: () => _showUserDialog(context),
          icon: const Icon(Icons.add),
          label: const Text('Novo usuario'),
        ),
        child: EntityTable(
          columns: const ['Nome', 'Email', 'Perfil', 'Ativo'],
          rows: [
            for (final user in users.cast<Map<String, dynamic>>())
              [
                user['fullName'],
                user['email'],
                user['roleName'],
                user['isActive'] == true ? 'Sim' : 'Nao',
              ],
          ],
        ),
      ),
    );
  }

  Future<void> _showUserDialog(BuildContext context) async {
    final roles = (await widget.api.getList(
      '/api/roles',
    )).cast<Map<String, dynamic>>();
    if (!context.mounted) return;
    final saved = await showDialog<bool>(
      context: context,
      builder: (_) => UserDialog(api: widget.api, roles: roles),
    );

    if (saved == true) {
      setState(() => _reload++);
    }
  }
}

class CustomersPage extends StatefulWidget {
  const CustomersPage({required this.api, required this.session, super.key});

  final ApiClient api;
  final Session session;

  @override
  State<CustomersPage> createState() => _CustomersPageState();
}

class _CustomersPageState extends State<CustomersPage> {
  int _reload = 0;

  @override
  Widget build(BuildContext context) {
    return DataFuture(
      key: ValueKey(_reload),
      future: widget.api.getList('/api/customers'),
      builder: (context, customers) => PageScaffold(
        title: 'Clientes',
        action: FilledButton.icon(
          onPressed: () => _showCustomerDialog(context),
          icon: const Icon(Icons.add),
          label: const Text('Novo cliente'),
        ),
        child: EntityTable(
          columns: const ['Nome', 'Documento', 'Telefone', 'Cidade', 'UF'],
          rows: [
            for (final customer in customers.cast<Map<String, dynamic>>())
              [
                customer['name'],
                customer['documentNumber'],
                customer['phone'],
                customer['city'],
                customer['state'],
              ],
          ],
        ),
      ),
    );
  }

  Future<void> _showCustomerDialog(BuildContext context) async {
    final saved = await showDialog<bool>(
      context: context,
      builder: (_) => CustomerDialog(api: widget.api, session: widget.session),
    );

    if (saved == true) {
      setState(() => _reload++);
    }
  }
}

class VehiclesPage extends StatefulWidget {
  const VehiclesPage({required this.api, required this.session, super.key});

  final ApiClient api;
  final Session session;

  @override
  State<VehiclesPage> createState() => _VehiclesPageState();
}

class _VehiclesPageState extends State<VehiclesPage> {
  int _reload = 0;

  @override
  Widget build(BuildContext context) {
    return DataFuture(
      key: ValueKey(_reload),
      future: widget.api.getList('/api/vehicles'),
      builder: (context, vehicles) => PageScaffold(
        title: 'Veiculos',
        action: FilledButton.icon(
          onPressed: () => _showVehicleDialog(context),
          icon: const Icon(Icons.add),
          label: const Text('Novo veiculo'),
        ),
        child: EntityTable(
          columns: const ['Cliente', 'Placa', 'Marca', 'Modelo', 'Ano'],
          rows: [
            for (final vehicle in vehicles.cast<Map<String, dynamic>>())
              [
                vehicle['customerName'],
                vehicle['plate'],
                vehicle['brand'],
                vehicle['model'],
                '${vehicle['modelYear'] ?? ''}',
              ],
          ],
        ),
      ),
    );
  }

  Future<void> _showVehicleDialog(BuildContext context) async {
    final customers = (await widget.api.getList(
      '/api/customers',
    )).cast<Map<String, dynamic>>();
    if (!context.mounted) return;
    final saved = await showDialog<bool>(
      context: context,
      builder: (_) => VehicleDialog(
        api: widget.api,
        session: widget.session,
        customers: customers,
      ),
    );

    if (saved == true) {
      setState(() => _reload++);
    }
  }
}

class ProductsPage extends StatefulWidget {
  const ProductsPage({required this.api, required this.session, super.key});

  final ApiClient api;
  final Session session;

  @override
  State<ProductsPage> createState() => _ProductsPageState();
}

class _ProductsPageState extends State<ProductsPage> {
  int _reload = 0;

  @override
  Widget build(BuildContext context) {
    return DataFuture(
      key: ValueKey(_reload),
      future: widget.api.getList('/api/products'),
      builder: (context, products) => PageScaffold(
        title: 'Produtos',
        action: FilledButton.icon(
          onPressed: () => _showProductDialog(context),
          icon: const Icon(Icons.add),
          label: const Text('Novo produto'),
        ),
        child: EntityTable(
          columns: const ['Codigo', 'Nome', 'Un.', 'Venda', 'Estoque'],
          rows: [
            for (final product in products.cast<Map<String, dynamic>>())
              [
                product['sku'],
                product['name'],
                product['unit'],
                "R\$ ${product['salePrice']}",
                '${product['quantity']}',
              ],
          ],
        ),
      ),
    );
  }

  Future<void> _showProductDialog(BuildContext context) async {
    final saved = await showDialog<bool>(
      context: context,
      builder: (_) => ProductDialog(api: widget.api, session: widget.session),
    );

    if (saved == true) {
      setState(() => _reload++);
    }
  }
}

class ServicesPage extends StatefulWidget {
  const ServicesPage({required this.api, required this.session, super.key});

  final ApiClient api;
  final Session session;

  @override
  State<ServicesPage> createState() => _ServicesPageState();
}

class _ServicesPageState extends State<ServicesPage> {
  int _reload = 0;

  @override
  Widget build(BuildContext context) {
    return DataFuture(
      key: ValueKey(_reload),
      future: widget.api.getList('/api/services'),
      builder: (context, services) => PageScaffold(
        title: 'Servicos',
        action: FilledButton.icon(
          onPressed: () => _showServiceDialog(context),
          icon: const Icon(Icons.add),
          label: const Text('Novo servico'),
        ),
        child: EntityTable(
          columns: const ['Codigo', 'Nome', 'Preco', 'Minutos', 'Ativo'],
          rows: [
            for (final service in services.cast<Map<String, dynamic>>())
              [
                service['code'],
                service['name'],
                "R\$ ${service['standardPrice']}",
                '${service['estimatedMinutes'] ?? ''}',
                service['isActive'] == true ? 'Sim' : 'Nao',
              ],
          ],
        ),
      ),
    );
  }

  Future<void> _showServiceDialog(BuildContext context) async {
    final saved = await showDialog<bool>(
      context: context,
      builder: (_) => ServiceDialog(api: widget.api, session: widget.session),
    );

    if (saved == true) {
      setState(() => _reload++);
    }
  }
}

class AuditPage extends StatelessWidget {
  const AuditPage({required this.api, super.key});

  final ApiClient api;

  @override
  Widget build(BuildContext context) {
    return DataFuture(
      future: api.getList('/api/audit-logs'),
      builder: (context, logs) => PageScaffold(
        title: 'Auditoria',
        child: EntityTable(
          columns: const ['Usuario', 'Entidade', 'Acao', 'Data'],
          rows: [
            for (final log in logs.cast<Map<String, dynamic>>())
              [
                log['userName'],
                log['entityName'],
                log['action'],
                log['createdAt'],
              ],
          ],
        ),
      ),
    );
  }
}

class DataFuture<T> extends StatelessWidget {
  const DataFuture({required this.future, required this.builder, super.key});

  final Future<T> future;
  final Widget Function(BuildContext context, T data) builder;

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<T>(
      future: future,
      builder: (context, snapshot) {
        if (snapshot.connectionState != ConnectionState.done) {
          return const Center(child: CircularProgressIndicator());
        }

        if (snapshot.hasError) {
          return Center(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Text(
                'Nao foi possivel carregar os dados da API.\n${snapshot.error}',
                textAlign: TextAlign.center,
              ),
            ),
          );
        }

        return builder(context, snapshot.data as T);
      },
    );
  }
}

class PageScaffold extends StatelessWidget {
  const PageScaffold({
    required this.title,
    required this.child,
    this.action,
    super.key,
  });

  final String title;
  final Widget child;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  title,
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
              ),
              if (action != null) action!,
            ],
          ),
          const SizedBox(height: 20),
          child,
        ],
      ),
    );
  }
}

class EntityTable extends StatelessWidget {
  const EntityTable({required this.columns, required this.rows, super.key});

  final List<String> columns;
  final List<List<Object?>> rows;

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(8),
        side: BorderSide(color: Theme.of(context).dividerColor),
      ),
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: DataTable(
          columns: [
            for (final column in columns) DataColumn(label: Text(column)),
          ],
          rows: [
            for (final row in rows)
              DataRow(
                cells: [
                  for (final value in row) DataCell(Text('${value ?? ''}')),
                ],
              ),
          ],
        ),
      ),
    );
  }
}

class SummaryCard extends StatelessWidget {
  const SummaryCard({
    required this.title,
    required this.value,
    required this.icon,
    super.key,
  });

  final String title;
  final String value;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 220,
      height: 128,
      child: Card(
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(8),
          side: BorderSide(color: Theme.of(context).dividerColor),
        ),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            children: [
              Icon(icon, size: 32),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(title, maxLines: 2, overflow: TextOverflow.ellipsis),
                    const SizedBox(height: 8),
                    Text(
                      value,
                      style: Theme.of(context).textTheme.headlineSmall,
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class UserDialog extends StatefulWidget {
  const UserDialog({required this.api, required this.roles, super.key});

  final ApiClient api;
  final List<Map<String, dynamic>> roles;

  @override
  State<UserDialog> createState() => _UserDialogState();
}

class _UserDialogState extends State<UserDialog> {
  final _name = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController(text: '123456');
  String? _roleId;

  @override
  void initState() {
    super.initState();
    _roleId = widget.roles.isEmpty ? null : widget.roles.first['id'] as String;
  }

  @override
  Widget build(BuildContext context) {
    return FormDialog(
      title: 'Novo usuario',
      fields: [
        TextField(
          controller: _name,
          decoration: const InputDecoration(labelText: 'Nome'),
        ),
        TextField(
          controller: _email,
          decoration: const InputDecoration(labelText: 'Email'),
        ),
        TextField(
          controller: _password,
          decoration: const InputDecoration(labelText: 'Senha temporaria'),
        ),
        DropdownButtonFormField<String>(
          initialValue: _roleId,
          items: [
            for (final role in widget.roles)
              DropdownMenuItem(
                value: role['id'] as String,
                child: Text(role['name'] as String),
              ),
          ],
          onChanged: (value) => setState(() => _roleId = value),
          decoration: const InputDecoration(labelText: 'Perfil'),
        ),
      ],
      onSave: () => widget.api.post('/api/users', {
        'fullName': _name.text,
        'email': _email.text,
        'password': _password.text,
        'roleId': _roleId,
      }),
    );
  }
}

class CustomerDialog extends StatelessWidget {
  CustomerDialog({required this.api, required this.session, super.key});

  final ApiClient api;
  final Session session;
  final _name = TextEditingController();
  final _document = TextEditingController();
  final _phone = TextEditingController();
  final _email = TextEditingController();
  final _city = TextEditingController();
  final _state = TextEditingController(text: 'SP');

  @override
  Widget build(BuildContext context) {
    return FormDialog(
      title: 'Novo cliente',
      fields: [
        TextField(
          controller: _name,
          decoration: const InputDecoration(labelText: 'Nome'),
        ),
        TextField(
          controller: _document,
          decoration: const InputDecoration(labelText: 'Documento'),
        ),
        TextField(
          controller: _phone,
          decoration: const InputDecoration(labelText: 'Telefone'),
        ),
        TextField(
          controller: _email,
          decoration: const InputDecoration(labelText: 'Email'),
        ),
        TextField(
          controller: _city,
          decoration: const InputDecoration(labelText: 'Cidade'),
        ),
        TextField(
          controller: _state,
          decoration: const InputDecoration(labelText: 'UF'),
        ),
      ],
      onSave: () => api.post('/api/customers', {
        'name': _name.text,
        'documentNumber': _document.text,
        'phone': _phone.text,
        'email': _email.text,
        'city': _city.text,
        'state': _state.text,
        'userId': session.userId,
      }),
    );
  }
}

class VehicleDialog extends StatefulWidget {
  const VehicleDialog({
    required this.api,
    required this.session,
    required this.customers,
    super.key,
  });

  final ApiClient api;
  final Session session;
  final List<Map<String, dynamic>> customers;

  @override
  State<VehicleDialog> createState() => _VehicleDialogState();
}

class _VehicleDialogState extends State<VehicleDialog> {
  final _plate = TextEditingController();
  final _brand = TextEditingController();
  final _model = TextEditingController();
  final _year = TextEditingController();
  String? _customerId;

  @override
  void initState() {
    super.initState();
    _customerId = widget.customers.isEmpty
        ? null
        : widget.customers.first['id'] as String;
  }

  @override
  Widget build(BuildContext context) {
    return FormDialog(
      title: 'Novo veiculo',
      fields: [
        DropdownButtonFormField<String>(
          initialValue: _customerId,
          items: [
            for (final customer in widget.customers)
              DropdownMenuItem(
                value: customer['id'] as String,
                child: Text(customer['name'] as String),
              ),
          ],
          onChanged: (value) => setState(() => _customerId = value),
          decoration: const InputDecoration(labelText: 'Cliente'),
        ),
        TextField(
          controller: _plate,
          decoration: const InputDecoration(labelText: 'Placa'),
        ),
        TextField(
          controller: _brand,
          decoration: const InputDecoration(labelText: 'Marca'),
        ),
        TextField(
          controller: _model,
          decoration: const InputDecoration(labelText: 'Modelo'),
        ),
        TextField(
          controller: _year,
          decoration: const InputDecoration(labelText: 'Ano'),
        ),
      ],
      onSave: () => widget.api.post('/api/vehicles', {
        'customerId': _customerId,
        'plate': _plate.text,
        'brand': _brand.text,
        'model': _model.text,
        'modelYear': int.tryParse(_year.text),
        'userId': widget.session.userId,
      }),
    );
  }
}

class ProductDialog extends StatelessWidget {
  ProductDialog({required this.api, required this.session, super.key});

  final ApiClient api;
  final Session session;
  final _sku = TextEditingController();
  final _name = TextEditingController();
  final _salePrice = TextEditingController(text: '0');
  final _costPrice = TextEditingController(text: '0');
  final _stock = TextEditingController(text: '0');

  @override
  Widget build(BuildContext context) {
    return FormDialog(
      title: 'Novo produto',
      fields: [
        TextField(
          controller: _sku,
          decoration: const InputDecoration(labelText: 'Codigo'),
        ),
        TextField(
          controller: _name,
          decoration: const InputDecoration(labelText: 'Nome'),
        ),
        TextField(
          controller: _salePrice,
          decoration: const InputDecoration(labelText: 'Preco venda'),
        ),
        TextField(
          controller: _costPrice,
          decoration: const InputDecoration(labelText: 'Custo'),
        ),
        TextField(
          controller: _stock,
          decoration: const InputDecoration(labelText: 'Estoque inicial'),
        ),
      ],
      onSave: () => api.post('/api/products', {
        'sku': _sku.text,
        'name': _name.text,
        'unit': 'UN',
        'salePrice': decimal(_salePrice.text),
        'costPrice': decimal(_costPrice.text),
        'minimumStock': 0,
        'initialStock': decimal(_stock.text),
        'userId': session.userId,
      }),
    );
  }
}

class ServiceDialog extends StatelessWidget {
  ServiceDialog({required this.api, required this.session, super.key});

  final ApiClient api;
  final Session session;
  final _code = TextEditingController();
  final _name = TextEditingController();
  final _price = TextEditingController(text: '0');
  final _minutes = TextEditingController();

  @override
  Widget build(BuildContext context) {
    return FormDialog(
      title: 'Novo servico',
      fields: [
        TextField(
          controller: _code,
          decoration: const InputDecoration(labelText: 'Codigo'),
        ),
        TextField(
          controller: _name,
          decoration: const InputDecoration(labelText: 'Nome'),
        ),
        TextField(
          controller: _price,
          decoration: const InputDecoration(labelText: 'Preco'),
        ),
        TextField(
          controller: _minutes,
          decoration: const InputDecoration(labelText: 'Minutos'),
        ),
      ],
      onSave: () => api.post('/api/services', {
        'code': _code.text,
        'name': _name.text,
        'standardPrice': decimal(_price.text),
        'estimatedMinutes': int.tryParse(_minutes.text),
        'userId': session.userId,
      }),
    );
  }
}

class FormDialog extends StatefulWidget {
  const FormDialog({
    required this.title,
    required this.fields,
    required this.onSave,
    super.key,
  });

  final String title;
  final List<Widget> fields;
  final Future<dynamic> Function() onSave;

  @override
  State<FormDialog> createState() => _FormDialogState();
}

class _FormDialogState extends State<FormDialog> {
  bool _saving = false;
  String? _error;

  Future<void> _save() async {
    setState(() {
      _saving = true;
      _error = null;
    });

    try {
      await widget.onSave();
      if (mounted) Navigator.of(context).pop(true);
    } catch (error) {
      setState(() => _error = 'Nao foi possivel salvar. $error');
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text(widget.title),
      content: SizedBox(
        width: 460,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              for (final field in widget.fields) ...[
                field,
                const SizedBox(height: 12),
              ],
              if (_error != null)
                Text(
                  _error!,
                  style: TextStyle(color: Theme.of(context).colorScheme.error),
                ),
            ],
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: _saving ? null : () => Navigator.of(context).pop(false),
          child: const Text('Cancelar'),
        ),
        FilledButton.icon(
          onPressed: _saving ? null : _save,
          icon: _saving
              ? const SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : const Icon(Icons.save),
          label: const Text('Salvar'),
        ),
      ],
    );
  }
}

double decimal(String value) {
  return double.tryParse(value.replaceAll(',', '.')) ?? 0;
}
