import "./App.css";
import { useEffect, useMemo, useState } from "react";
import { getHome, getPickupLocations, getProducts, getTheme } from "./api";
import CartDrawer from "./components/CartDrawer";
import CheckoutPanel from "./components/CheckoutPanel";
import Hero from "./components/Hero";
import ProductCard from "./components/ProductCard";

function App() {
  const [products, setProducts] = useState([]);
  const [pickupLocations, setPickupLocations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [home, setHome] = useState(null);
  const [theme, setTheme] = useState(null);
  const [cartOpen, setCartOpen] = useState(false);
  const [checkoutOpen, setCheckoutOpen] = useState(false);
  const [orderId, setOrderId] = useState("");
  const [cart, setCart] = useState([]);

  useEffect(() => {
    let alive = true;
    async function load() {
      try {
        const [p, h, pickups, t] = await Promise.all([
          getProducts(),
          getHome(),
          getPickupLocations(),
          getTheme(),
        ]);
        if (!alive) return;
        setProducts(p.products || []);
        setHome(h.home || null);
        setPickupLocations(pickups || []);
        setTheme(t.theme || null);
      } finally {
        if (alive) setLoading(false);
      }
    }
    load();
    return () => {
      alive = false;
    };
  }, []);

  useEffect(() => {
    if (!theme) return;
    const root = document.documentElement;
    const map = {
      "--bg": theme.background,
      "--bg-accent": theme.backgroundAccent,
      "--card": theme.card,
      "--card-soft": theme.cardSoft,
      "--text": theme.text,
      "--muted": theme.muted,
      "--accent": theme.accent,
      "--accent-2": theme.accent2,
      "--topbar-bg": theme.topbarBg,
      "--topbar-border": theme.topbarBorder,
      "--hero-gradient-1": theme.heroGradient1,
      "--hero-gradient-2": theme.heroGradient2,
      "--hero-gradient-3": theme.heroGradient3,
      "--hero-overlay-1": theme.heroOverlay1,
      "--hero-overlay-2": theme.heroOverlay2,
      "--glow": theme.glow,
      "--shadow": theme.shadow,
    };
    Object.entries(map).forEach(([key, value]) => {
      if (value) root.style.setProperty(key, value);
    });
  }, [theme]);

  useEffect(() => {
    let alive = true;
    const interval = setInterval(async () => {
      try {
        const p = await getProducts();
        if (!alive) return;
        setProducts(p.products || []);
      } catch {
        // ignore refresh errors
      }
    }, 60000);
    return () => {
      alive = false;
      clearInterval(interval);
    };
  }, []);

  const cartItems = useMemo(() => cart, [cart]);

  function addToCart(product) {
    setCart((prev) => {
      const existing = prev.find((p) => p.slug === product.slug);
      if (existing) {
        return prev.map((p) =>
          p.slug === product.slug ? { ...p, quantity: p.quantity + 1 } : p
        );
      }
      return [
        ...prev,
        {
          slug: product.slug,
          name: product.name,
          priceCents: product.priceCents,
          quantity: 1,
        },
      ];
    });
    setCartOpen(true);
  }

  function updateQty(slug, qty) {
    setCart((prev) =>
      prev
        .map((p) => (p.slug === slug ? { ...p, quantity: qty } : p))
        .filter((p) => p.quantity > 0)
    );
  }

  function handleCheckoutComplete(id) {
    setOrderId(id);
    setCheckoutOpen(false);
    setCartOpen(false);
    setCart([]);
  }

  return (
    <div className="app">
      <header className="topbar">
        <div className="brand">NutsShop</div>
        <div className="actions">
          <button className="btn ghost" onClick={() => setCartOpen(true)}>
            Cart ({cartItems.length})
          </button>
        </div>
      </header>

      <main>
        <Hero hero={home} />
        <section className="section">
          <div className="section-head">
            <h2>Featured Nuts</h2>
            <p className="muted">Fresh batches, roasted locally in Cape Town.</p>
          </div>

          {loading ? (
            <div className="loading">Loading the crunch...</div>
          ) : (
            <div className="grid">
              {(home?.featuredProducts?.length
                ? home.featuredProducts
                : products
              ).map((p) => (
                <ProductCard key={p.slug} product={p} onAdd={addToCart} />
              ))}
            </div>
          )}
        </section>

        {orderId ? (
          <section className="section success">
            <h3>Order placed</h3>
            <p className="muted">Order ID: {orderId}</p>
          </section>
        ) : null}
      </main>

      {cartOpen ? (
        <div className="overlay">
          {checkoutOpen ? (
            <CheckoutPanel
              items={cartItems}
              pickupLocations={pickupLocations}
              onBack={() => setCheckoutOpen(false)}
              onComplete={handleCheckoutComplete}
            />
          ) : (
            <CartDrawer
              items={cartItems}
              onClose={() => setCartOpen(false)}
              onUpdateQty={updateQty}
              onCheckout={() => setCheckoutOpen(true)}
            />
          )}
        </div>
      ) : null}
    </div>
  );
}

export default App;
