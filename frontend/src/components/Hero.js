export default function Hero({ hero }) {
  const title = hero?.heroTitle || "Snack Better in Cape Town";
  const subtitle =
    hero?.heroSubtitle ||
    "Fresh roasted nuts, trail mixes, and premium crunch — delivered or pickup.";
  const promo =
    hero?.promoText || "Cape Town only • ZAR pricing • Same-day pickup options";
  const heroImage = hero?.heroImageUrl || "";

  return (
    <section
      className={`hero ${heroImage ? "hero-image" : ""}`}
      style={heroImage ? { backgroundImage: `url(${heroImage})` } : undefined}
    >
      <div className="hero-glow" />
      <div className="hero-content">
        <p className="eyebrow">Cape Town Only</p>
        <h1>{title}</h1>
        <p className="hero-sub">{subtitle}</p>
        <p className="hero-sub muted" style={{ marginTop: 12 }}>
          {promo}
        </p>
      </div>
    </section>
  );
}
