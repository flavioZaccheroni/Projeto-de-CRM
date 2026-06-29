import 'package:desktop/main.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('shows CRM dashboard shell', (WidgetTester tester) async {
    await tester.pumpWidget(const AutoPartsCrmApp());

    expect(find.text('CRM Autopecas e Servicos'), findsWidgets);
    expect(find.text('Painel inicial'), findsOneWidget);
    expect(find.text('Clientes'), findsWidgets);
    expect(find.text('Ordens abertas'), findsOneWidget);
  });
}
