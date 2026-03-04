create table if not exists site_pages (
  id integer primary key,
  delivery_title text not null,
  delivery_content text not null,
  about_title text not null,
  about_content text not null,
  contact_title text not null,
  contact_content text not null,
  testimonials jsonb not null
);

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

create or replace function upsert_site_pages(
  p_delivery_title text,
  p_delivery_content text,
  p_about_title text,
  p_about_content text,
  p_contact_title text,
  p_contact_content text,
  p_testimonials jsonb
) returns void
language plpgsql as $$
begin
  insert into site_pages(
    id, delivery_title, delivery_content, about_title, about_content,
    contact_title, contact_content, testimonials
  )
  values (
    1, p_delivery_title, p_delivery_content, p_about_title, p_about_content,
    p_contact_title, p_contact_content, p_testimonials
  )
  on conflict (id) do update set
    delivery_title = excluded.delivery_title,
    delivery_content = excluded.delivery_content,
    about_title = excluded.about_title,
    about_content = excluded.about_content,
    contact_title = excluded.contact_title,
    contact_content = excluded.contact_content,
    testimonials = excluded.testimonials;
end;
$$;

create or replace function get_site_pages()
returns table(
  delivery_title text,
  delivery_content text,
  about_title text,
  about_content text,
  contact_title text,
  contact_content text,
  testimonials jsonb
) language sql as $$
  select
    delivery_title,
    delivery_content,
    about_title,
    about_content,
    contact_title,
    contact_content,
    testimonials
  from site_pages
  where id = 1;
$$;
