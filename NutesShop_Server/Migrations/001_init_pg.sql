create extension if not exists "pgcrypto";

create table if not exists products (
  slug text primary key,
  name text not null,
  description text not null,
  price_cents integer not null,
  per text not null,
  image_url text not null,
  in_stock boolean not null,
  featured boolean not null,
  badges text[] not null
);

create table if not exists home_page (
  id integer primary key,
  hero_title text not null,
  hero_subtitle text not null,
  promo_text text not null,
  hero_image_url text not null,
  featured_products jsonb not null
);

create table if not exists orders (
  id uuid primary key,
  created_at timestamptz not null default now(),
  customer_name text not null,
  customer_email text not null,
  fulfillment_type text not null,
  city text not null,
  suburb text null,
  address_line1 text null,
  address_line2 text null,
  postal_code text null,
  pickup_location text null,
  status text not null,
  total_cents integer not null
);

create table if not exists order_items (
  id uuid primary key,
  order_id uuid not null references orders(id) on delete cascade,
  product_slug text not null,
  product_name text not null,
  unit_price_cents integer not null,
  quantity integer not null
);

create or replace function upsert_product(
  p_slug text,
  p_name text,
  p_description text,
  p_price_cents integer,
  p_per text,
  p_image_url text,
  p_in_stock boolean,
  p_featured boolean,
  p_badges text[]
) returns void
language plpgsql as $$
begin
  insert into products(slug, name, description, price_cents, per, image_url, in_stock, featured, badges)
  values (p_slug, p_name, p_description, p_price_cents, p_per, p_image_url, p_in_stock, p_featured, p_badges)
  on conflict (slug) do update set
    name = excluded.name,
    description = excluded.description,
    price_cents = excluded.price_cents,
    per = excluded.per,
    image_url = excluded.image_url,
    in_stock = excluded.in_stock,
    featured = excluded.featured,
    badges = excluded.badges;
end;
$$;

create or replace function upsert_home(
  p_hero_title text,
  p_hero_subtitle text,
  p_promo_text text,
  p_hero_image_url text,
  p_featured_products jsonb
) returns void
language plpgsql as $$
begin
  insert into home_page(id, hero_title, hero_subtitle, promo_text, hero_image_url, featured_products)
  values (1, p_hero_title, p_hero_subtitle, p_promo_text, p_hero_image_url, p_featured_products)
  on conflict (id) do update set
    hero_title = excluded.hero_title,
    hero_subtitle = excluded.hero_subtitle,
    promo_text = excluded.promo_text,
    hero_image_url = excluded.hero_image_url,
    featured_products = excluded.featured_products;
end;
$$;

create or replace function get_products()
returns table(
  slug text,
  name text,
  description text,
  price_cents integer,
  per text,
  image_url text,
  in_stock boolean,
  featured boolean,
  badges text[]
) language sql as $$
  select slug, name, description, price_cents, per, image_url, in_stock, featured, badges
  from products
  order by name;
$$;

create or replace function get_product(p_slug text)
returns table(
  slug text,
  name text,
  description text,
  price_cents integer,
  per text,
  image_url text,
  in_stock boolean,
  featured boolean,
  badges text[]
) language sql as $$
  select slug, name, description, price_cents, per, image_url, in_stock, featured, badges
  from products
  where slug = p_slug;
$$;

create or replace function get_home()
returns table(
  hero_title text,
  hero_subtitle text,
  promo_text text,
  hero_image_url text,
  featured_products jsonb
) language sql as $$
  select hero_title, hero_subtitle, promo_text, hero_image_url, featured_products
  from home_page
  where id = 1;
$$;

create or replace function create_order(
  p_customer_name text,
  p_customer_email text,
  p_fulfillment_type text,
  p_city text,
  p_suburb text,
  p_address_line1 text,
  p_address_line2 text,
  p_postal_code text,
  p_pickup_location text,
  p_total_cents integer
) returns uuid
language plpgsql as $$
declare
  v_id uuid := gen_random_uuid();
begin
  insert into orders(
    id, customer_name, customer_email, fulfillment_type, city, suburb,
    address_line1, address_line2, postal_code, pickup_location, status, total_cents
  )
  values (
    v_id, p_customer_name, p_customer_email, p_fulfillment_type, p_city, p_suburb,
    p_address_line1, p_address_line2, p_postal_code, p_pickup_location, 'Placed', p_total_cents
  );
  return v_id;
end;
$$;

create or replace function add_order_item(
  p_order_id uuid,
  p_product_slug text,
  p_product_name text,
  p_unit_price_cents integer,
  p_quantity integer
) returns void
language plpgsql as $$
begin
  insert into order_items(id, order_id, product_slug, product_name, unit_price_cents, quantity)
  values (gen_random_uuid(), p_order_id, p_product_slug, p_product_name, p_unit_price_cents, p_quantity);
end;
$$;
