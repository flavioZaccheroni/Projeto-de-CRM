insert into work_orders (
  id,
  customer_id,
  vehicle_id,
  opened_by_user_id,
  assigned_to_user_id,
  status,
  problem_description,
  technical_notes,
  total_products,
  total_services,
  total_amount
) values (
  '00000000-0000-0000-0012-000000000001',
  '00000000-0000-0000-0003-000000000001',
  '00000000-0000-0000-0004-000000000001',
  '00000000-0000-0000-0002-000000000001',
  '00000000-0000-0000-0002-000000000003',
  'open',
  'Revisao preventiva com troca de filtros.',
  'Aguardando entrada na oficina.',
  45.00,
  80.00,
  125.00
) on conflict (id) do nothing;

insert into work_order_items (
  work_order_id,
  item_type,
  product_id,
  description,
  quantity,
  unit_price,
  total_amount
)
select
  '00000000-0000-0000-0012-000000000001',
  'product',
  '00000000-0000-0000-0005-000000000001',
  'Filtro de oleo',
  1,
  45.00,
  45.00
where not exists (
  select 1
  from work_order_items
  where work_order_id = '00000000-0000-0000-0012-000000000001'
    and product_id = '00000000-0000-0000-0005-000000000001'
);

insert into work_order_items (
  work_order_id,
  item_type,
  service_id,
  description,
  quantity,
  unit_price,
  total_amount
)
select
  '00000000-0000-0000-0012-000000000001',
  'service',
  '00000000-0000-0000-0006-000000000001',
  'Troca de oleo',
  1,
  80.00,
  80.00
where not exists (
  select 1
  from work_order_items
  where work_order_id = '00000000-0000-0000-0012-000000000001'
    and service_id = '00000000-0000-0000-0006-000000000001'
);
