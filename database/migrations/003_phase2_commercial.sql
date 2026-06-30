create table customer_interactions (
  id uuid primary key default uuid_generate_v4(),
  customer_id uuid not null references customers(id),
  user_id uuid references users(id),
  interaction_type varchar(30) not null,
  subject varchar(160) not null,
  description text,
  status varchar(30) not null default 'open',
  next_contact_at timestamptz,
  created_at timestamptz not null default now(),
  constraint customer_interactions_type_check check (interaction_type in ('call', 'whatsapp', 'email', 'visit', 'counter', 'other')),
  constraint customer_interactions_status_check check (status in ('open', 'done', 'cancelled'))
);

create table sales_orders (
  id uuid primary key default uuid_generate_v4(),
  customer_id uuid not null references customers(id),
  quotation_id uuid references quotations(id),
  created_by_user_id uuid not null references users(id),
  status varchar(30) not null default 'draft',
  notes text,
  total_products numeric(14,2) not null default 0,
  total_services numeric(14,2) not null default 0,
  total_amount numeric(14,2) not null default 0,
  created_at timestamptz not null default now(),
  updated_at timestamptz,
  constraint sales_orders_status_check check (status in ('draft', 'confirmed', 'cancelled', 'invoiced'))
);

create table sales_order_items (
  id uuid primary key default uuid_generate_v4(),
  sales_order_id uuid not null references sales_orders(id) on delete cascade,
  item_type varchar(20) not null,
  product_id uuid references products(id),
  service_id uuid references services(id),
  description varchar(220) not null,
  quantity numeric(14,3) not null,
  unit_price numeric(14,2) not null,
  total_amount numeric(14,2) not null,
  constraint sales_order_items_type_check check (item_type in ('product', 'service')),
  constraint sales_order_items_quantity_positive check (quantity > 0),
  constraint sales_order_items_price_non_negative check (unit_price >= 0 and total_amount >= 0),
  constraint sales_order_items_reference_check check (
    (item_type = 'product' and product_id is not null and service_id is null)
    or
    (item_type = 'service' and service_id is not null and product_id is null)
  )
);

insert into permissions (id, code, description) values
  ('00000000-0000-0000-0001-000000000009', 'interactions.write', 'Registrar atendimentos'),
  ('00000000-0000-0000-0001-000000000010', 'sales_orders.write', 'Criar e alterar pedidos de venda')
on conflict (code) do nothing;

insert into role_permissions (role_id, permission_id)
select r.id, p.id
from roles r
cross join permissions p
where r.name in ('Administrador', 'Gerencia', 'Vendedor')
  and p.code in ('interactions.write', 'sales_orders.write')
on conflict do nothing;

create index idx_customer_interactions_customer_id on customer_interactions(customer_id);
create index idx_customer_interactions_created_at on customer_interactions(created_at);
create index idx_sales_orders_customer_id on sales_orders(customer_id);
create index idx_sales_orders_status on sales_orders(status);
