import { useMemo, useState } from "react";
import { checkout } from "../api";

const defaultDelivery = {
  city: "Cape Town",
  suburb: "",
  addressLine1: "",
  addressLine2: "",
  postalCode: "",
};

export default function CheckoutPanel({
  items,
  pickupLocations,
  onComplete,
  onBack,
}) {
  const [mode, setMode] = useState("Delivery");
  const [delivery, setDelivery] = useState(defaultDelivery);
  const [pickup, setPickup] = useState(
    pickupLocations[0]?.id || "CT_WATERFRONT"
  );
  const [customer, setCustomer] = useState({
    name: "",
    email: "",
  });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  const payload = useMemo(() => {
    return {
      customerName: customer.name,
      customerEmail: customer.email,
      fulfillmentType: mode,
      delivery: mode === "Delivery" ? delivery : null,
      pickup: mode === "Pickup" ? { locationId: pickup } : null,
      items: items.map((i) => ({
        productSlug: i.slug,
        quantity: i.quantity,
      })),
    };
  }, [customer, delivery, items, mode, pickup]);

  async function handleSubmit(e) {
    e.preventDefault();
    setBusy(true);
    setError("");
    try {
      const res = await checkout(payload);
      onComplete(res.orderId);
    } catch (err) {
      setError(err.message || "Checkout failed");
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="checkout" onSubmit={handleSubmit}>
      <div className="drawer-header">
        <h2>Checkout</h2>
        <button type="button" className="btn ghost" onClick={onBack}>
          Back
        </button>
      </div>

      <div className="checkout-body">
        <div className="segment">
          <label>Full name</label>
          <input
            value={customer.name}
            onChange={(e) => setCustomer({ ...customer, name: e.target.value })}
            required
          />
        </div>
        <div className="segment">
          <label>Email</label>
          <input
            type="email"
            value={customer.email}
            onChange={(e) => setCustomer({ ...customer, email: e.target.value })}
            required
          />
        </div>

        <div className="segment">
          <label>Fulfillment</label>
          <div className="toggle">
            <button
              type="button"
              className={mode === "Delivery" ? "active" : ""}
              onClick={() => setMode("Delivery")}
            >
              Delivery
            </button>
            <button
              type="button"
              className={mode === "Pickup" ? "active" : ""}
              onClick={() => setMode("Pickup")}
            >
              Pickup
            </button>
          </div>
        </div>

        {mode === "Delivery" ? (
          <>
            <div className="segment">
              <label>City</label>
              <input value="Cape Town" readOnly />
            </div>
            <div className="segment">
              <label>Suburb</label>
              <input
                value={delivery.suburb}
                onChange={(e) =>
                  setDelivery({ ...delivery, suburb: e.target.value })
                }
                required
              />
            </div>
            <div className="segment">
              <label>Address line 1</label>
              <input
                value={delivery.addressLine1}
                onChange={(e) =>
                  setDelivery({ ...delivery, addressLine1: e.target.value })
                }
                required
              />
            </div>
            <div className="segment">
              <label>Address line 2</label>
              <input
                value={delivery.addressLine2}
                onChange={(e) =>
                  setDelivery({ ...delivery, addressLine2: e.target.value })
                }
              />
            </div>
            <div className="segment">
              <label>Postal code</label>
              <input
                value={delivery.postalCode}
                onChange={(e) =>
                  setDelivery({ ...delivery, postalCode: e.target.value })
                }
                required
              />
            </div>
          </>
        ) : (
          <div className="segment">
            <label>Pickup location</label>
            <select value={pickup} onChange={(e) => setPickup(e.target.value)}>
              {pickupLocations.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name}
                </option>
              ))}
            </select>
          </div>
        )}

        {error ? <div className="error">{error}</div> : null}
      </div>

      <div className="drawer-footer">
        <button className="btn primary" disabled={busy}>
          {busy ? "Placing order..." : "Place order"}
        </button>
      </div>
    </form>
  );
}
