create table purchase_orders (
  id uuid primary key default uuid_generate_v4(),
  supplier_id uuid references customers(id),
  created_by_user_id uuid not null references users(id),
  status varchar(30) not null default 'draft',
  expected_at date,
  notes text,
  total_amount numeric(14,2) not null default 0,
  created_at timestamptz not null default now(),
  updated_at timestamptz,
  constraint purchase_orders_status_check check (status in ('draft', 'ordered', 'received', 'cancelled'))
);

create table purchase_order_items (
  id uuid primary key default uuid_generate_v4(),
  purchase_order_id uuid not null references purchase_orders(id) on delete cascade,
  product_id uuid not null references products(id),
  description varchar(220) not null,
  quantity numeric(14,3) not null,
  unit_cost numeric(14,2) not null,
  total_amount numeric(14,2) not null,
  received_quantity numeric(14,3) not null default 0,
  constraint purchase_order_items_quantity_positive check (quantity > 0),
  constraint purchase_order_items_cost_non_negative check (unit_cost >= 0 and total_amount >= 0),
  constraint purchase_order_items_received_non_negative check (received_quantity >= 0)
);

create table purchase_receipts (
  id uuid primary key default uuid_generate_v4(),
  purchase_order_id uuid not null references purchase_orders(id),
  user_id uuid not null references users(id),
  notes text,
  created_at timestamptz not null default now()
);

create table purchase_receipt_items (
  id uuid primary key default uuid_generate_v4(),
  purchase_receipt_id uuid not null references purchase_receipts(id) on delete cascade,
  purchase_order_item_id uuid not null references purchase_order_items(id),
  product_id uuid not null references products(id),
  quantity numeric(14,3) not null,
  constraint purchase_receipt_items_quantity_positive check (quantity > 0)
);

insert into permissions (id, code, description) values
  ('00000000-0000-0000-0001-000000000011', 'purchase_orders.write', 'Criar e receber pedidos de compra'),
  ('00000000-0000-0000-0001-000000000012', 'stock.read', 'Consultar estoque e movimentacoes')
on conflict (code) do nothing;

insert into role_permissions (role_id, permission_id)
select r.id, p.id
from roles r
cross join permissions p
where r.name in ('Administrador', 'Gerencia', 'Estoquista')
  and p.code in ('purchase_orders.write', 'stock.read')
on conflict do nothing;

create index idx_purchase_orders_status on purchase_orders(status);
create index idx_purchase_orders_created_at on purchase_orders(created_at);
create index idx_purchase_order_items_order_id on purchase_order_items(purchase_order_id);
create index idx_purchase_receipts_order_id on purchase_receipts(purchase_order_id);
