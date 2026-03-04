export default function Hero({ hero }) {
  const title = hero?.heroTitle || "Fresh Nuts. Smart Snacks. Delivered Easy.";
  const subtitle =
    hero?.heroSubtitle ||
    "Shop roasted nuts, trail mixes, and healthy pantry snacks with a simple checkout.";
  const promo =
    hero?.promoText ||
    "Cape Town delivery and pickup • Secure ordering • Friendly service";
  const heroImage = hero?.heroImageUrl || "";

  return (
    <section
      className={`hero ${heroImage ? "hero-image" : ""}`}
      style={heroImage ? { backgroundImage: `url(${heroImage})` } : undefined}
    >
      <div className="hero-glow" />
      <div className="hero-content">
        <p className="eyebrow">Healthy Snack Shop</p>
        <h1>{title}</h1>
        <p className="hero-sub">{subtitle}</p>
        <p className="hero-sub muted" style={{ marginTop: 12 }}>
          {promo}
        </p>
        <div className="hero-pills">
          <span>100% quality nuts</span>
          <span>Fresh weekly batches</span>
          <span>Easy checkout</span>
        </div>
      </div>
    </section>
  );
}
