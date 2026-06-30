insert into customer_interactions (id, customer_id, user_id, interaction_type, subject, description, status)
values (
  '00000000-0000-0000-0009-000000000001',
  '00000000-0000-0000-0003-000000000001',
  '00000000-0000-0000-0002-000000000002',
  'whatsapp',
  'Retorno sobre orcamento aprovado',
  'Cliente confirmou interesse na troca de oleo e pediu previsao de horario.',
  'done'
)
on conflict (id) do nothing;

insert into sales_orders (
  id,
  customer_id,
  quotation_id,
  created_by_user_id,
  status,
  notes,
  total_products,
  total_services,
  total_amount
) values (
  '00000000-0000-0000-0010-000000000001',
  '00000000-0000-0000-0003-000000000001',
  '00000000-0000-0000-0007-000000000001',
  '00000000-0000-0000-0002-000000000002',
  'confirmed',
  'Pedido ficticio criado a partir do orcamento aprovado.',
  124.80,
  80.00,
  204.80
)
on conflict (id) do nothing;

insert into sales_order_items (sales_order_id, item_type, product_id, description, quantity, unit_price, total_amount)
values
  ('00000000-0000-0000-0010-000000000001', 'product', '00000000-0000-0000-0005-000000000001', 'Filtro de oleo', 1, 45.00, 45.00),
  ('00000000-0000-0000-0010-000000000001', 'product', '00000000-0000-0000-0005-000000000002', 'Oleo motor 5W30', 2, 39.90, 79.80)
on conflict do nothing;

insert into sales_order_items (sales_order_id, item_type, service_id, description, quantity, unit_price, total_amount)
values
  ('00000000-0000-0000-0010-000000000001', 'service', '00000000-0000-0000-0006-000000000001', 'Troca de oleo', 1, 80.00, 80.00)
on conflict do nothing;
