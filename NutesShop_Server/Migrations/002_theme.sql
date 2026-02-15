create table if not exists theme (
  id integer primary key,
  name text not null,
  background text not null,
  background_accent text not null,
  card text not null,
  card_soft text not null,
  text text not null,
  muted text not null,
  accent text not null,
  accent_2 text not null,
  topbar_bg text not null,
  topbar_border text not null,
  hero_gradient_1 text not null,
  hero_gradient_2 text not null,
  hero_gradient_3 text not null,
  hero_overlay_1 text not null,
  hero_overlay_2 text not null,
  glow text not null,
  shadow text not null
);

create or replace function upsert_theme(
  p_name text,
  p_background text,
  p_background_accent text,
  p_card text,
  p_card_soft text,
  p_text text,
  p_muted text,
  p_accent text,
  p_accent_2 text,
  p_topbar_bg text,
  p_topbar_border text,
  p_hero_gradient_1 text,
  p_hero_gradient_2 text,
  p_hero_gradient_3 text,
  p_hero_overlay_1 text,
  p_hero_overlay_2 text,
  p_glow text,
  p_shadow text
) returns void
language plpgsql as $$
begin
  insert into theme(
    id, name, background, background_accent, card, card_soft, text, muted,
    accent, accent_2, topbar_bg, topbar_border,
    hero_gradient_1, hero_gradient_2, hero_gradient_3,
    hero_overlay_1, hero_overlay_2, glow, shadow
  )
  values (
    1, p_name, p_background, p_background_accent, p_card, p_card_soft, p_text, p_muted,
    p_accent, p_accent_2, p_topbar_bg, p_topbar_border,
    p_hero_gradient_1, p_hero_gradient_2, p_hero_gradient_3,
    p_hero_overlay_1, p_hero_overlay_2, p_glow, p_shadow
  )
  on conflict (id) do update set
    name = excluded.name,
    background = excluded.background,
    background_accent = excluded.background_accent,
    card = excluded.card,
    card_soft = excluded.card_soft,
    text = excluded.text,
    muted = excluded.muted,
    accent = excluded.accent,
    accent_2 = excluded.accent_2,
    topbar_bg = excluded.topbar_bg,
    topbar_border = excluded.topbar_border,
    hero_gradient_1 = excluded.hero_gradient_1,
    hero_gradient_2 = excluded.hero_gradient_2,
    hero_gradient_3 = excluded.hero_gradient_3,
    hero_overlay_1 = excluded.hero_overlay_1,
    hero_overlay_2 = excluded.hero_overlay_2,
    glow = excluded.glow,
    shadow = excluded.shadow;
end;
$$;

create or replace function get_theme()
returns table(
  name text,
  background text,
  background_accent text,
  card text,
  card_soft text,
  text text,
  muted text,
  accent text,
  accent_2 text,
  topbar_bg text,
  topbar_border text,
  hero_gradient_1 text,
  hero_gradient_2 text,
  hero_gradient_3 text,
  hero_overlay_1 text,
  hero_overlay_2 text,
  glow text,
  shadow text
) language sql as $$
  select name, background, background_accent, card, card_soft, text, muted,
         accent, accent_2, topbar_bg, topbar_border,
         hero_gradient_1, hero_gradient_2, hero_gradient_3,
         hero_overlay_1, hero_overlay_2, glow, shadow
  from theme
  where id = 1;
$$;
