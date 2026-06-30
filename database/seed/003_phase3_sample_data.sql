insert into purchase_orders (
  id,
  created_by_user_id,
  status,
  expected_at,
  notes,
  total_amount
) values (
  '00000000-0000-0000-0011-000000000001',
  '00000000-0000-0000-0002-000000000001',
  'ordered',
  current_date + interval '7 days',
  'Pedido de compra ficticio para reposicao inicial.',
  515.00
)
on conflict (id) do nothing;

insert into purchase_order_items (
  id,
  purchase_order_id,
  product_id,
  description,
  quantity,
  unit_cost,
  total_amount
) values
  (
    '00000000-0000-0000-0012-000000000001',
    '00000000-0000-0000-0011-000000000001',
    '00000000-0000-0000-0005-000000000001',
    'Filtro de oleo',
    10,
    25.00,
    250.00
  ),
  (
    '00000000-0000-0000-0012-000000000002',
    '00000000-0000-0000-0011-000000000001',
    '00000000-0000-0000-0005-000000000002',
    'Oleo motor 5W30',
    10,
    26.50,
    265.00
  )
on conflict (id) do nothing;
