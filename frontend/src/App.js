import "./App.css";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  Link,
  Navigate,
  NavLink,
  Route,
  Routes,
  useNavigate,
  useParams,
} from "react-router-dom";
import {
  formatZar,
  getHome,
  getOrder,
  getPages,
  getPickupLocations,
  getProduct,
  getProducts,
  getTheme,
  searchProducts,
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
const SEARCH_DEBOUNCE_MS = 300;

function ProductDetailsPage({ onAdd }) {
  const { slug = "" } = useParams();
  const [product, setProduct] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let alive = true;
    setLoading(true);
    setError("");

    async function load() {
      try {
        const res = await getProduct(slug);
        if (!alive) return;
        setProduct(res.product || null);
        if (!res.product) {
          setError("Product not found.");
        }
      } catch {
        if (alive) setError("Could not load product details.");
      } finally {
        if (alive) setLoading(false);
      }
    }

    load();
    return () => {
      alive = false;
    };
  }, [slug]);

  return (
    <section className="section route-page">
      <Link to="/" className="back-link">
        Back to catalog
      </Link>
      {loading ? (
        <div className="loading">Loading product...</div>
      ) : error || !product ? (
        <div className="error">{error || "Product not found."}</div>
      ) : (
        <article className="product-detail">
          <div className="product-detail-media">
            {product.imageUrl ? (
              <img src={product.imageUrl} alt={product.name} className="product-detail-image" />
            ) : (
              <div className="card-image placeholder">No image</div>
            )}
          </div>
          <div className="product-detail-body">
            <p className="price-chip">
              from {formatZar(product.priceCents)} / {product.per}
            </p>
            <h2>{product.name}</h2>
            <p className="muted">{product.description}</p>
            <div className="badge-row">
              {(product.badges || []).map((b, i) => (
                <span key={`${b}-${i}`} className="badge">
                  {b}
                </span>
              ))}
            </div>
            <div className="product-detail-actions">
              <span className={`stock ${product.inStock ? "in" : "out"}`}>
                {product.inStock ? "In stock" : "Out of stock"}
              </span>
              <button
                className="btn"
                disabled={!product.inStock}
                onClick={() => onAdd(product)}
              >
                Add to cart
              </button>
            </div>
          </div>
        </article>
      )}
    </section>
  );
}

function App() {
  const [products, setProducts] = useState([]);
  const [pickupLocations, setPickupLocations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [catalogLoading, setCatalogLoading] = useState(false);
  const [home, setHome] = useState(null);
  const [pages, setPages] = useState(null);
  const [theme, setTheme] = useState(null);
  const [cartOpen, setCartOpen] = useState(false);
  const [checkoutOpen, setCheckoutOpen] = useState(false);
  const [orderId, setOrderId] = useState("");
  const [orderDetails, setOrderDetails] = useState(null);
  const [cart, setCart] = useState([]);

  const [page, setPage] = useState(1);
  const [pageSize] = useState(12);
  const [totalPages, setTotalPages] = useState(0);
  const [totalProducts, setTotalProducts] = useState(0);
  const [catalogQueryInput, setCatalogQueryInput] = useState("");
  const [catalogQuery, setCatalogQuery] = useState("");
  const catalogReadyRef = useRef(false);
  const catalogRequestRef = useRef(0);

  const [searchInput, setSearchInput] = useState("");
  const [searchSuggestions, setSearchSuggestions] = useState([]);
  const [searchLoading, setSearchLoading] = useState(false);
  const [showSearchDropdown, setShowSearchDropdown] = useState(false);
  const searchBoxRef = useRef(null);

  const navigate = useNavigate();

  const loadCatalog = useCallback(async (nextPage, query, keepLoading = true) => {
    const requestId = ++catalogRequestRef.current;
    if (keepLoading) setCatalogLoading(true);
    try {
      const p = await getProducts({ page: nextPage, pageSize, q: query });
      if (requestId !== catalogRequestRef.current) return;
      setProducts(p.products || []);
      setPage(p.page || nextPage);
      setTotalPages(p.totalPages || 0);
      setTotalProducts(p.total || 0);
    } finally {
      if (keepLoading && requestId === catalogRequestRef.current) {
        setCatalogLoading(false);
      }
    }
  }, [pageSize]);

  useEffect(() => {
    let alive = true;
    async function load() {
      try {
        const [h, pickups, t, pg] = await Promise.all([
          getHome(),
          getPickupLocations(),
          getTheme(),
          getPages(),
        ]);
        if (!alive) return;
        setHome(h.home || null);
        setPickupLocations(pickups || []);
        setTheme(t.theme || null);
        setPages(pg.pages || null);
        await loadCatalog(1, "", false);
      } finally {
        if (alive) {
          catalogReadyRef.current = true;
          setLoading(false);
        }
      }
    }
    load();
    return () => {
      alive = false;
    };
  }, [loadCatalog]);

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
        const p = await getProducts({ page, pageSize, q: catalogQuery });
        if (!alive) return;
        setProducts(p.products || []);
        setTotalPages(p.totalPages || 0);
        setTotalProducts(p.total || 0);
      } catch {
        // ignore refresh errors
      }
    }, 60000);
    return () => {
      alive = false;
      clearInterval(interval);
    };
  }, [page, pageSize, catalogQuery]);

  useEffect(() => {
    if (!catalogReadyRef.current) return;
    const q = catalogQueryInput.trim();
    const timeoutId = setTimeout(() => {
      if (q === catalogQuery) return;
      setCatalogQuery(q);
      loadCatalog(1, q);
    }, SEARCH_DEBOUNCE_MS);

    return () => clearTimeout(timeoutId);
  }, [catalogQueryInput, catalogQuery, loadCatalog]);

  useEffect(() => {
    const handler = (event) => {
      if (searchBoxRef.current && !searchBoxRef.current.contains(event.target)) {
        setShowSearchDropdown(false);
      }
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  useEffect(() => {
    const q = searchInput.trim();
    if (q.length < 2) {
      setSearchSuggestions([]);
      setSearchLoading(false);
      return;
    }

    let alive = true;
    const controller = new AbortController();
    setSearchLoading(true);
    const timeoutId = setTimeout(async () => {
      try {
        const res = await searchProducts(q, 8, { signal: controller.signal });
        if (!alive) return;
        setSearchSuggestions(res.products || []);
      } catch {
        if (alive) setSearchSuggestions([]);
      } finally {
        if (alive) setSearchLoading(false);
      }
    }, SEARCH_DEBOUNCE_MS);

    return () => {
      alive = false;
      controller.abort();
      clearTimeout(timeoutId);
    };
  }, [searchInput]);

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

  async function handleCatalogSearchSubmit(event) {
    event.preventDefault();
    const nextQuery = catalogQueryInput.trim();
    setCatalogQuery(nextQuery);
    await loadCatalog(1, nextQuery);
  }

  async function handleCatalogPageChange(nextPage) {
    await loadCatalog(nextPage, catalogQuery);
  }

  function handleSuggestionClick(slug) {
    setShowSearchDropdown(false);
    setSearchInput("");
    setSearchSuggestions([]);
    navigate(`/products/${slug}`);
  }

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
          <div className="search-box" ref={searchBoxRef}>
            <input
              type="search"
              className="search-input"
              placeholder="Search products..."
              value={searchInput}
              onFocus={() => setShowSearchDropdown(true)}
              onChange={(e) => setSearchInput(e.target.value)}
            />
            {showSearchDropdown ? (
              <div className="search-dropdown">
                {searchLoading ? (
                  <div className="search-empty">Searching...</div>
                ) : searchSuggestions.length > 0 ? (
                  searchSuggestions.map((p) => (
                    <button
                      key={p.slug}
                      className="search-item"
                      onClick={() => handleSuggestionClick(p.slug)}
                    >
                      <span>{p.name}</span>
                      <small>{formatZar(p.priceCents)}</small>
                    </button>
                  ))
                ) : searchInput.trim().length >= 2 ? (
                  <div className="search-empty">No products found</div>
                ) : (
                  <div className="search-empty">Type at least 2 letters</div>
                )}
              </div>
            ) : null}
          </div>
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

                  <form className="catalog-filter" onSubmit={handleCatalogSearchSubmit}>
                    <input
                      type="search"
                      value={catalogQueryInput}
                      onChange={(e) => setCatalogQueryInput(e.target.value)}
                      placeholder="Filter catalog"
                    />
                    <button className="btn small" type="submit">
                      Filter
                    </button>
                    <button
                      className="btn small ghost"
                      type="button"
                      onClick={async () => {
                        setCatalogQueryInput("");
                        setCatalogQuery("");
                        await loadCatalog(1, "");
                      }}
                    >
                      Clear
                    </button>
                    <span className="muted">{totalProducts} products</span>
                  </form>

                  {loading || catalogLoading ? (
                    <div className="loading">Loading the crunch...</div>
                  ) : (
                    <>
                      <div className="grid">
                        {products.map((p) => (
                          <ProductCard key={p.slug} product={p} onAdd={addToCart} />
                        ))}
                      </div>
                      <div className="pagination">
                        <button
                          className="btn small ghost"
                          disabled={page <= 1}
                          onClick={() => handleCatalogPageChange(page - 1)}
                        >
                          Previous
                        </button>
                        <span className="muted">
                          Page {totalPages === 0 ? 0 : page} of {totalPages}
                        </span>
                        <button
                          className="btn small ghost"
                          disabled={page >= totalPages}
                          onClick={() => handleCatalogPageChange(page + 1)}
                        >
                          Next
                        </button>
                      </div>
                    </>
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

          <Route path="/products/:slug" element={<ProductDetailsPage onAdd={addToCart} />} />

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
