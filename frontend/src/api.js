const API_BASE = process.env.REACT_APP_API_BASE_URL || "";

async function request(path, options) {
  const res = await fetch(`${API_BASE}${path}`, options);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || "Request failed");
  }
  return res.json();
}

export async function getProducts() {
  return request("/api/shop/products");
}

export async function getHome() {
  return request("/api/shop/home");
}

export async function getTheme() {
  return request("/api/shop/theme");
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

export function formatZar(cents) {
  const rands = cents / 100;
  return new Intl.NumberFormat("en-ZA", {
    style: "currency",
    currency: "ZAR",
  }).format(rands);
}
