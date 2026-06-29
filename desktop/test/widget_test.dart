import 'package:desktop/main.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('shows CRM login shell', (WidgetTester tester) async {
    await tester.pumpWidget(const AutoPartsCrmApp());

    expect(find.text('CRM Autopecas e Servicos'), findsWidgets);
    expect(find.text('Acesso ao ambiente de desenvolvimento'), findsOneWidget);
    expect(find.text('Entrar'), findsOneWidget);
  });
}
