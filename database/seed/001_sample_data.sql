insert into roles (id, name, description) values
  ('00000000-0000-0000-0000-000000000001', 'Administrador', 'Acesso total ao sistema'),
  ('00000000-0000-0000-0000-000000000002', 'Gerencia', 'Acesso gerencial e relatorios'),
  ('00000000-0000-0000-0000-000000000003', 'Vendedor', 'Atendimento, clientes e orcamentos'),
  ('00000000-0000-0000-0000-000000000004', 'Estoquista', 'Produtos e movimentacoes de estoque'),
  ('00000000-0000-0000-0000-000000000005', 'Tecnico', 'Ordens de servico'),
  ('00000000-0000-0000-0000-000000000006', 'Financeiro', 'Rotinas financeiras futuras');

insert into permissions (id, code, description) values
  ('00000000-0000-0000-0001-000000000001', 'customers.read', 'Consultar clientes'),
  ('00000000-0000-0000-0001-000000000002', 'customers.write', 'Cadastrar e alterar clientes'),
  ('00000000-0000-0000-0001-000000000003', 'products.read', 'Consultar produtos'),
  ('00000000-0000-0000-0001-000000000004', 'products.write', 'Cadastrar e alterar produtos'),
  ('00000000-0000-0000-0001-000000000005', 'stock.move', 'Movimentar estoque'),
  ('00000000-0000-0000-0001-000000000006', 'quotations.write', 'Criar e alterar orcamentos'),
  ('00000000-0000-0000-0001-000000000007', 'work_orders.write', 'Criar e alterar ordens de servico'),
  ('00000000-0000-0000-0001-000000000008', 'reports.read', 'Consultar relatorios');

insert into role_permissions (role_id, permission_id)
select '00000000-0000-0000-0000-000000000001', id from permissions;

insert into role_permissions (role_id, permission_id) values
  ('00000000-0000-0000-0000-000000000002', '00000000-0000-0000-0001-000000000001'),
  ('00000000-0000-0000-0000-000000000002', '00000000-0000-0000-0001-000000000003'),
  ('00000000-0000-0000-0000-000000000002', '00000000-0000-0000-0001-000000000008'),
  ('00000000-0000-0000-0000-000000000003', '00000000-0000-0000-0001-000000000001'),
  ('00000000-0000-0000-0000-000000000003', '00000000-0000-0000-0001-000000000002'),
  ('00000000-0000-0000-0000-000000000003', '00000000-0000-0000-0001-000000000003'),
  ('00000000-0000-0000-0000-000000000003', '00000000-0000-0000-0001-000000000006'),
  ('00000000-0000-0000-0000-000000000004', '00000000-0000-0000-0001-000000000003'),
  ('00000000-0000-0000-0000-000000000004', '00000000-0000-0000-0001-000000000004'),
  ('00000000-0000-0000-0000-000000000004', '00000000-0000-0000-0001-000000000005'),
  ('00000000-0000-0000-0000-000000000005', '00000000-0000-0000-0001-000000000001'),
  ('00000000-0000-0000-0000-000000000005', '00000000-0000-0000-0001-000000000003'),
  ('00000000-0000-0000-0000-000000000005', '00000000-0000-0000-0001-000000000007');

insert into users (id, role_id, full_name, email, password_hash) values
  ('00000000-0000-0000-0002-000000000001', '00000000-0000-0000-0000-000000000001', 'Administrador Sistema', 'admin@crm.local', 'trocar-por-hash-real'),
  ('00000000-0000-0000-0002-000000000002', '00000000-0000-0000-0000-000000000003', 'Vendedor Teste', 'vendedor@crm.local', 'trocar-por-hash-real'),
  ('00000000-0000-0000-0002-000000000003', '00000000-0000-0000-0000-000000000005', 'Tecnico Teste', 'tecnico@crm.local', 'trocar-por-hash-real');

insert into customers (id, name, document_number, phone, email, address_line, city, state, notes) values
  ('00000000-0000-0000-0003-000000000001', 'Cliente Exemplo Ltda', '12345678000199', '(11) 4000-1000', 'cliente@example.com', 'Rua das Oficinas, 100', 'Sao Paulo', 'SP', 'Cliente ficticio para testes do MVP.');

insert into vehicles (id, customer_id, plate, brand, model, model_year, color, notes) values
  ('00000000-0000-0000-0004-000000000001', '00000000-0000-0000-0003-000000000001', 'ABC1D23', 'Fiat', 'Strada', 2021, 'Branca', 'Veiculo ficticio para validacao de ordem de servico.');

insert into products (id, sku, name, description, unit, sale_price, cost_price, minimum_stock) values
  ('00000000-0000-0000-0005-000000000001', 'FIL-OLEO-001', 'Filtro de oleo', 'Filtro de oleo para teste operacional.', 'UN', 45.00, 25.00, 5),
  ('00000000-0000-0000-0005-000000000002', 'OLEO-5W30-001', 'Oleo motor 5W30', 'Oleo sintetico para teste operacional.', 'UN', 39.90, 24.50, 12);

insert into stock_balances (product_id, quantity) values
  ('00000000-0000-0000-0005-000000000001', 20),
  ('00000000-0000-0000-0005-000000000002', 48);

insert into services (id, code, name, description, standard_price, estimated_minutes) values
  ('00000000-0000-0000-0006-000000000001', 'SERV-TROCA-OLEO', 'Troca de oleo', 'Servico de troca de oleo para teste do MVP.', 80.00, 45);

insert into quotations (id, customer_id, vehicle_id, created_by_user_id, status, total_products, total_services, total_amount, notes) values
  ('00000000-0000-0000-0007-000000000001', '00000000-0000-0000-0003-000000000001', '00000000-0000-0000-0004-000000000001', '00000000-0000-0000-0002-000000000002', 'approved', 124.80, 80.00, 204.80, 'Orcamento ficticio aprovado para teste.');

insert into quotation_items (quotation_id, item_type, product_id, description, quantity, unit_price, total_amount) values
  ('00000000-0000-0000-0007-000000000001', 'product', '00000000-0000-0000-0005-000000000001', 'Filtro de oleo', 1, 45.00, 45.00),
  ('00000000-0000-0000-0007-000000000001', 'product', '00000000-0000-0000-0005-000000000002', 'Oleo motor 5W30', 2, 39.90, 79.80);

insert into quotation_items (quotation_id, item_type, service_id, description, quantity, unit_price, total_amount) values
  ('00000000-0000-0000-0007-000000000001', 'service', '00000000-0000-0000-0006-000000000001', 'Troca de oleo', 1, 80.00, 80.00);

insert into work_orders (id, customer_id, vehicle_id, quotation_id, opened_by_user_id, assigned_to_user_id, status, problem_description, total_products, total_services, total_amount) values
  ('00000000-0000-0000-0008-000000000001', '00000000-0000-0000-0003-000000000001', '00000000-0000-0000-0004-000000000001', '00000000-0000-0000-0007-000000000001', '00000000-0000-0000-0002-000000000002', '00000000-0000-0000-0002-000000000003', 'in_progress', 'Cliente solicitou troca de oleo preventiva.', 124.80, 80.00, 204.80);

insert into work_order_items (work_order_id, item_type, product_id, description, quantity, unit_price, total_amount) values
  ('00000000-0000-0000-0008-000000000001', 'product', '00000000-0000-0000-0005-000000000001', 'Filtro de oleo', 1, 45.00, 45.00),
  ('00000000-0000-0000-0008-000000000001', 'product', '00000000-0000-0000-0005-000000000002', 'Oleo motor 5W30', 2, 39.90, 79.80);

insert into work_order_items (work_order_id, item_type, service_id, description, quantity, unit_price, total_amount) values
  ('00000000-0000-0000-0008-000000000001', 'service', '00000000-0000-0000-0006-000000000001', 'Troca de oleo', 1, 80.00, 80.00);

insert into stock_movements (product_id, work_order_id, user_id, movement_type, quantity, reason) values
  ('00000000-0000-0000-0005-000000000001', '00000000-0000-0000-0008-000000000001', '00000000-0000-0000-0002-000000000003', 'out', 1, 'Uso em ordem de servico de teste'),
  ('00000000-0000-0000-0005-000000000002', '00000000-0000-0000-0008-000000000001', '00000000-0000-0000-0002-000000000003', 'out', 2, 'Uso em ordem de servico de teste');

update stock_balances
set quantity = quantity - 1, updated_at = now()
where product_id = '00000000-0000-0000-0005-000000000001';

update stock_balances
set quantity = quantity - 2, updated_at = now()
where product_id = '00000000-0000-0000-0005-000000000002';
