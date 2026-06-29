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
      home: const HomeShell(),
    );
  }
}

class HomeShell extends StatelessWidget {
  const HomeShell({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Row(
        children: [
          NavigationRail(
            extended: true,
            selectedIndex: 0,
            destinations: [
              NavigationRailDestination(
                icon: Icon(Icons.dashboard_outlined),
                selectedIcon: Icon(Icons.dashboard),
                label: Text('Painel'),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.people_outline),
                selectedIcon: Icon(Icons.people),
                label: Text('Clientes'),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.inventory_2_outlined),
                selectedIcon: Icon(Icons.inventory_2),
                label: Text('Estoque'),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.car_repair_outlined),
                selectedIcon: Icon(Icons.car_repair),
                label: Text('Ordens'),
              ),
            ],
          ),
          const VerticalDivider(width: 1),
          Expanded(
            child: CustomScrollView(
              slivers: [
                SliverAppBar(
                  pinned: true,
                  title: const Text('CRM Autopecas e Servicos'),
                  actions: [
                    IconButton(
                      tooltip: 'Sincronizar',
                      onPressed: () {},
                      icon: const Icon(Icons.sync),
                    ),
                    IconButton(
                      tooltip: 'Usuario',
                      onPressed: () {},
                      icon: const Icon(Icons.account_circle_outlined),
                    ),
                    const SizedBox(width: 12),
                  ],
                ),
                const SliverPadding(
                  padding: EdgeInsets.all(24),
                  sliver: SliverToBoxAdapter(child: DashboardContent()),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class DashboardContent extends StatelessWidget {
  const DashboardContent({super.key});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Painel inicial',
          style: Theme.of(context).textTheme.headlineSmall,
        ),
        const SizedBox(height: 4),
        Text(
          'Base desktop criada para validar o MVP operacional.',
          style: Theme.of(context).textTheme.bodyMedium,
        ),
        const SizedBox(height: 24),
        const Wrap(
          spacing: 16,
          runSpacing: 16,
          children: [
            SummaryCard(title: 'Clientes', value: '1', icon: Icons.people),
            SummaryCard(title: 'Produtos', value: '2', icon: Icons.inventory_2),
            SummaryCard(
              title: 'Orcamentos',
              value: '1',
              icon: Icons.receipt_long,
            ),
            SummaryCard(
              title: 'Ordens abertas',
              value: '1',
              icon: Icons.car_repair,
            ),
          ],
        ),
        const SizedBox(height: 28),
        Text(
          'Proximas implementacoes',
          style: Theme.of(context).textTheme.titleLarge,
        ),
        const SizedBox(height: 12),
        const TaskList(),
      ],
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

class TaskList extends StatelessWidget {
  const TaskList({super.key});

  @override
  Widget build(BuildContext context) {
    const tasks = [
      'Conectar login com a API',
      'Criar cadastro de clientes',
      'Criar cadastro de produtos',
      'Implementar orcamento simples',
      'Implementar ordem de servico',
      'Registrar baixa de estoque',
    ];

    return Column(
      children: [
        for (final task in tasks)
          ListTile(
            leading: const Icon(Icons.check_circle_outline),
            title: Text(task),
            contentPadding: EdgeInsets.zero,
          ),
      ],
    );
  }
}
