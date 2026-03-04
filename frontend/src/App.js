import "./App.css";
import { useEffect, useMemo, useState } from "react";
import { Navigate, NavLink, Route, Routes } from "react-router-dom";
import {
  formatZar,
  getHome,
  getOrder,
  getPages,
  getPickupLocations,
  getProducts,
  getTheme,
} from "./api";
import CartDrawer from "./components/CartDrawer";
import CheckoutPanel from "./components/CheckoutPanel";
import Hero from "./components/Hero";
import ProductCard from "./components/ProductCard";

const defaultTestimonials = [
  {
    name: "Lindiwe Jacobs",
    feedback:
      "Always fresh and consistent. The mixed nuts are now a permanent office snack.",
    imageUrl: "",
  },
  {
    name: "Ryan Daniels",
    feedback:
      "Fast delivery in Cape Town and quality is excellent. The roasted almonds are top tier.",
    imageUrl: "",
  },
  {
    name: "Nadia Petersen",
    feedback:
      "Great healthy option for lunchboxes. Ordering is simple and support is responsive.",
    imageUrl: "",
  },
];

function App() {
  const [products, setProducts] = useState([]);
  const [pickupLocations, setPickupLocations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [home, setHome] = useState(null);
  const [pages, setPages] = useState(null);
  const [theme, setTheme] = useState(null);
  const [cartOpen, setCartOpen] = useState(false);
  const [checkoutOpen, setCheckoutOpen] = useState(false);
  const [orderId, setOrderId] = useState("");
  const [orderDetails, setOrderDetails] = useState(null);
  const [cart, setCart] = useState([]);

  useEffect(() => {
    let alive = true;
    async function load() {
      try {
        const [p, h, pickups, t, pg] = await Promise.all([
          getProducts(),
          getHome(),
          getPickupLocations(),
          getTheme(),
          getPages(),
        ]);
        if (!alive) return;
        setProducts(p.products || []);
        setHome(h.home || null);
        setPickupLocations(pickups || []);
        setTheme(t.theme || null);
        setPages(pg.pages || null);
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

  async function handleCheckoutComplete(id, order) {
    setOrderId(id);
    setOrderDetails(order);
    setCheckoutOpen(false);
    setCartOpen(false);
    setCart([]);
  }

  useEffect(() => {
    let alive = true;
    if (!orderId || orderDetails) return () => {};

    async function loadOrder() {
      try {
        const res = await getOrder(orderId);
        if (!alive) return;
        setOrderDetails(res.order || null);
      } catch {
        // ignore order fetch errors
      }
    }

    loadOrder();
    return () => {
      alive = false;
    };
  }, [orderId, orderDetails]);

  const pageContent = {
    deliveryTitle: pages?.deliveryTitle || "Delivery",
    deliveryContent:
      pages?.deliveryContent ||
      "We currently deliver across Cape Town. Place your order before 16:00 for next-day delivery.",
    aboutTitle: pages?.aboutTitle || "About Us",
    aboutContent:
      pages?.aboutContent ||
      "NutsShop sources premium nuts and healthy snacks, roasted and packed fresh for every order.",
    contactTitle: pages?.contactTitle || "Contact",
    contactContent:
      pages?.contactContent ||
      "Need help with an order? Reach us at support@nuteshop.local.",
    testimonials:
      pages?.testimonials?.length > 0 ? pages.testimonials : defaultTestimonials,
  };

  return (
    <div className="app">
      <header className="topbar">
        <div className="brand">
          <span className="brand-mark">NS</span>
          <span>NutsShop</span>
        </div>
        <nav className="topnav">
          <NavLink
            to="/"
            end
            className={({ isActive }) =>
              `topnav-link${isActive ? " active" : ""}`
            }
          >
            Catalog
          </NavLink>
          <NavLink
            to="/delivery"
            className={({ isActive }) =>
              `topnav-link${isActive ? " active" : ""}`
            }
          >
            Delivery
          </NavLink>
          <NavLink
            to="/contact"
            className={({ isActive }) =>
              `topnav-link${isActive ? " active" : ""}`
            }
          >
            Contact
          </NavLink>
          <NavLink
            to="/about"
            className={({ isActive }) =>
              `topnav-link${isActive ? " active" : ""}`
            }
          >
            About
          </NavLink>
        </nav>
        <div className="actions">
          <button className="icon-btn" aria-label="Search">
            Search
          </button>
          <button className="btn ghost" onClick={() => setCartOpen(true)}>
            Cart ({cartItems.length})
          </button>
        </div>
      </header>

      <main>
        <Routes>
          <Route
            path="/"
            element={
              <>
                <Hero hero={home} />
                <section className="section" id="catalog">
                  <div className="section-head">
                    <h2>Popular Healthy Snacks</h2>
                    <p className="muted">
                      Roasted fresh, packed with care, and ready for quick
                      checkout.
                    </p>
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
                    {orderDetails ? (
                      <>
                        <p className="muted">
                          Status: {orderDetails.status} | Total:{" "}
                          {formatZar(orderDetails.totalCents)}
                        </p>
                        {orderDetails.items?.length ? (
                          <div className="order-items">
                            {orderDetails.items.map((item) => (
                              <div
                                key={item.productSlug}
                                className="order-item-row"
                              >
                                <span>
                                  {item.quantity} x {item.productName}
                                </span>
                                <strong>
                                  {formatZar(
                                    item.unitPriceCents * item.quantity
                                  )}
                                </strong>
                              </div>
                            ))}
                          </div>
                        ) : null}
                      </>
                    ) : null}
                  </section>
                ) : null}
              </>
            }
          />

          <Route
            path="/delivery"
            element={
              <section className="section route-page">
                <div className="section-head">
                  <h2>{pageContent.deliveryTitle}</h2>
                </div>
                <div className="info-card">
                  {pageContent.deliveryContent.split("\n").map((line, index) => (
                    <p key={`${line}-${index}`} className="muted">
                      {line}
                    </p>
                  ))}
                </div>
              </section>
            }
          />

          <Route
            path="/contact"
            element={
              <section className="section route-page">
                <div className="section-head">
                  <h2>{pageContent.contactTitle}</h2>
                </div>
                <div className="info-card">
                  {pageContent.contactContent.split("\n").map((line, index) => (
                    <p key={`${line}-${index}`} className="muted">
                      {line}
                    </p>
                  ))}
                </div>
              </section>
            }
          />

          <Route
            path="/about"
            element={
              <section className="section route-page">
                <div className="section-head">
                  <h2>{pageContent.aboutTitle}</h2>
                </div>
                <div className="info-card">
                  {pageContent.aboutContent.split("\n").map((line, index) => (
                    <p key={`${line}-${index}`} className="muted">
                      {line}
                    </p>
                  ))}
                </div>

                <div className="section-head testimonials-head">
                  <h3>Testimonials</h3>
                </div>
                <div className="testimonials-grid">
                  {pageContent.testimonials.map((t, index) => (
                    <article
                      key={`${t.name}-${index}`}
                      className="testimonial-card"
                    >
                      <div className="testimonial-head">
                        {t.imageUrl ? (
                          <img
                            src={t.imageUrl}
                            alt={t.name || "Customer"}
                            className="testimonial-avatar"
                          />
                        ) : (
                          <span className="testimonial-avatar placeholder">
                            {(t.name || "C").slice(0, 1).toUpperCase()}
                          </span>
                        )}
                        <p className="testimonial-name">{t.name}</p>
                      </div>
                      <p className="testimonial-quote">"{t.feedback}"</p>
                    </article>
                  ))}
                </div>
              </section>
            }
          />

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
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
