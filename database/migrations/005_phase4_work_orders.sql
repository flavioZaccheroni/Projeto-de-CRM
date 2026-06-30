alter table sales_orders
  add column if not exists work_order_id uuid references work_orders(id);

insert into permissions (code, description) values
  ('work_orders.read', 'Consultar ordens de servico')
on conflict (code) do nothing;

insert into role_permissions (role_id, permission_id)
select r.id, p.id
from roles r
cross join permissions p
where r.name in ('Administrador', 'Gerencia', 'Vendedor', 'Tecnico')
  and p.code in ('work_orders.read', 'work_orders.write')
on conflict do nothing;

create index if not exists idx_sales_orders_work_order_id on sales_orders(work_order_id);
create index if not exists idx_work_orders_assigned_to_user_id on work_orders(assigned_to_user_id);
create index if not exists idx_work_orders_opened_at on work_orders(opened_at);
