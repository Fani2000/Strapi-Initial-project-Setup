const API_BASE = process.env.REACT_APP_API_BASE_URL || "";

async function request(path, options) {
  const res = await fetch(`${API_BASE}${path}`, options);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || "Request failed");
  }
  return res.json();
}

export async function getProducts({ page = 1, pageSize = 12, q = "" } = {}) {
  const params = new URLSearchParams();
  params.set("page", String(page));
  params.set("pageSize", String(pageSize));
  if (q) params.set("q", q);
  return request(`/api/shop/products?${params.toString()}`);
}

export async function searchProducts(q, limit = 8, options = {}) {
  const params = new URLSearchParams();
  params.set("q", q);
  params.set("limit", String(limit));
  return request(`/api/shop/products/search?${params.toString()}`, options);
}

export async function getProduct(slug) {
  return request(`/api/shop/products/${slug}`);
}

export async function getHome() {
  return request("/api/shop/home");
}

export async function getTheme() {
  return request("/api/shop/theme");
}

export async function getPages() {
  return request("/api/shop/pages");
}

export async function getPickupLocations() {
  return request("/api/shop/pickup-locations");
}

export async function checkout(payload) {
  return request("/api/shop/checkout", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
}

export async function getOrder(orderId) {
  return request(`/api/shop/orders/${orderId}`);
}

export function formatZar(cents) {
  const rands = cents / 100;
  return new Intl.NumberFormat("en-ZA", {
    style: "currency",
    currency: "ZAR",
  }).format(rands);
}
