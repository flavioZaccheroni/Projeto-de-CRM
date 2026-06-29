create extension if not exists "uuid-ossp";

create table roles (
  id uuid primary key default uuid_generate_v4(),
  name varchar(80) not null unique,
  description text,
  created_at timestamptz not null default now()
);

create table permissions (
  id uuid primary key default uuid_generate_v4(),
  code varchar(120) not null unique,
  description text not null
);

create table role_permissions (
  role_id uuid not null references roles(id) on delete cascade,
  permission_id uuid not null references permissions(id) on delete cascade,
  primary key (role_id, permission_id)
);

create table users (
  id uuid primary key default uuid_generate_v4(),
  role_id uuid not null references roles(id),
  full_name varchar(160) not null,
  email varchar(180) not null unique,
  password_hash text not null,
  is_active boolean not null default true,
  created_at timestamptz not null default now(),
  updated_at timestamptz
);

create table customers (
  id uuid primary key default uuid_generate_v4(),
  name varchar(180) not null,
  document_number varchar(30),
  phone varchar(30),
  email varchar(180),
  address_line varchar(220),
  city varchar(100),
  state varchar(2),
  notes text,
  is_active boolean not null default true,
  created_at timestamptz not null default now(),
  updated_at timestamptz
);

create table vehicles (
  id uuid primary key default uuid_generate_v4(),
  customer_id uuid not null references customers(id),
  plate varchar(12),
  brand varchar(80),
  model varchar(100),
  model_year integer,
  color varchar(60),
  vin varchar(40),
  notes text,
  created_at timestamptz not null default now(),
  updated_at timestamptz
);

create table products (
  id uuid primary key default uuid_generate_v4(),
  sku varchar(60) not null unique,
  name varchar(180) not null,
  description text,
  unit varchar(20) not null default 'UN',
  sale_price numeric(14,2) not null default 0,
  cost_price numeric(14,2) not null default 0,
  minimum_stock numeric(14,3) not null default 0,
  is_active boolean not null default true,
  created_at timestamptz not null default now(),
  updated_at timestamptz,
  constraint products_prices_non_negative check (sale_price >= 0 and cost_price >= 0)
);

create table services (
  id uuid primary key default uuid_generate_v4(),
  code varchar(60) not null unique,
  name varchar(180) not null,
  description text,
  standard_price numeric(14,2) not null default 0,
  estimated_minutes integer,
  is_active boolean not null default true,
  created_at timestamptz not null default now(),
  updated_at timestamptz,
  constraint services_price_non_negative check (standard_price >= 0)
);

create table stock_balances (
  product_id uuid primary key references products(id),
  quantity numeric(14,3) not null default 0,
  updated_at timestamptz not null default now(),
  constraint stock_balance_non_negative check (quantity >= 0)
);

create table quotations (
  id uuid primary key default uuid_generate_v4(),
  customer_id uuid not null references customers(id),
  vehicle_id uuid references vehicles(id),
  created_by_user_id uuid not null references users(id),
  status varchar(30) not null default 'draft',
  notes text,
  total_products numeric(14,2) not null default 0,
  total_services numeric(14,2) not null default 0,
  total_amount numeric(14,2) not null default 0,
  created_at timestamptz not null default now(),
  updated_at timestamptz,
  constraint quotations_status_check check (status in ('draft', 'sent', 'approved', 'rejected', 'expired', 'cancelled'))
);

create table quotation_items (
  id uuid primary key default uuid_generate_v4(),
  quotation_id uuid not null references quotations(id) on delete cascade,
  item_type varchar(20) not null,
  product_id uuid references products(id),
  service_id uuid references services(id),
  description varchar(220) not null,
  quantity numeric(14,3) not null,
  unit_price numeric(14,2) not null,
  total_amount numeric(14,2) not null,
  constraint quotation_items_type_check check (item_type in ('product', 'service')),
  constraint quotation_items_quantity_positive check (quantity > 0),
  constraint quotation_items_price_non_negative check (unit_price >= 0 and total_amount >= 0),
  constraint quotation_items_reference_check check (
    (item_type = 'product' and product_id is not null and service_id is null)
    or
    (item_type = 'service' and service_id is not null and product_id is null)
  )
);

create table work_orders (
  id uuid primary key default uuid_generate_v4(),
  customer_id uuid not null references customers(id),
  vehicle_id uuid references vehicles(id),
  quotation_id uuid references quotations(id),
  opened_by_user_id uuid not null references users(id),
  assigned_to_user_id uuid references users(id),
  status varchar(30) not null default 'open',
  problem_description text,
  technical_notes text,
  total_products numeric(14,2) not null default 0,
  total_services numeric(14,2) not null default 0,
  total_amount numeric(14,2) not null default 0,
  opened_at timestamptz not null default now(),
  closed_at timestamptz,
  updated_at timestamptz,
  constraint work_orders_status_check check (status in ('open', 'approved', 'in_progress', 'paused', 'completed', 'cancelled'))
);

create table work_order_items (
  id uuid primary key default uuid_generate_v4(),
  work_order_id uuid not null references work_orders(id) on delete cascade,
  item_type varchar(20) not null,
  product_id uuid references products(id),
  service_id uuid references services(id),
  description varchar(220) not null,
  quantity numeric(14,3) not null,
  unit_price numeric(14,2) not null,
  total_amount numeric(14,2) not null,
  constraint work_order_items_type_check check (item_type in ('product', 'service')),
  constraint work_order_items_quantity_positive check (quantity > 0),
  constraint work_order_items_price_non_negative check (unit_price >= 0 and total_amount >= 0),
  constraint work_order_items_reference_check check (
    (item_type = 'product' and product_id is not null and service_id is null)
    or
    (item_type = 'service' and service_id is not null and product_id is null)
  )
);

create table stock_movements (
  id uuid primary key default uuid_generate_v4(),
  product_id uuid not null references products(id),
  work_order_id uuid references work_orders(id),
  user_id uuid not null references users(id),
  movement_type varchar(20) not null,
  quantity numeric(14,3) not null,
  reason varchar(160) not null,
  created_at timestamptz not null default now(),
  constraint stock_movements_type_check check (movement_type in ('in', 'out', 'adjustment')),
  constraint stock_movements_quantity_positive check (quantity > 0)
);

create table audit_logs (
  id uuid primary key default uuid_generate_v4(),
  user_id uuid references users(id),
  entity_name varchar(100) not null,
  entity_id uuid,
  action varchar(80) not null,
  old_data jsonb,
  new_data jsonb,
  created_at timestamptz not null default now()
);

create index idx_customers_name on customers(name);
create index idx_vehicles_customer_id on vehicles(customer_id);
create index idx_products_name on products(name);
create index idx_quotations_customer_id on quotations(customer_id);
create index idx_work_orders_customer_id on work_orders(customer_id);
create index idx_work_orders_status on work_orders(status);
create index idx_stock_movements_product_id on stock_movements(product_id);
create index idx_audit_logs_entity on audit_logs(entity_name, entity_id);
