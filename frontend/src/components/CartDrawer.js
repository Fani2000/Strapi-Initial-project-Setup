import { formatZar } from "../api";

export default function CartDrawer({ items, onClose, onUpdateQty, onCheckout }) {
  const total = items.reduce(
    (sum, it) => sum + it.priceCents * it.quantity,
    0
  );

  return (
    <div className="drawer">
      <div className="drawer-header">
        <h2>Your Cart</h2>
        <button className="btn ghost" onClick={onClose}>
          Close
        </button>
      </div>
      <div className="drawer-body">
        {items.length === 0 ? (
          <p className="muted">No items yet.</p>
        ) : (
          items.map((it) => (
            <div key={it.slug} className="cart-row">
              <div>
                <div className="cart-name">{it.name}</div>
                <div className="muted">{formatZar(it.priceCents)}</div>
              </div>
              <div className="qty">
                <button
                  className="btn small ghost"
                  onClick={() => onUpdateQty(it.slug, it.quantity - 1)}
                >
                  -
                </button>
                <span>{it.quantity}</span>
                <button
                  className="btn small ghost"
                  onClick={() => onUpdateQty(it.slug, it.quantity + 1)}
                >
                  +
                </button>
              </div>
            </div>
          ))
        )}
      </div>
      <div className="drawer-footer">
        <div className="total">
          <span>Total</span>
          <strong>{formatZar(total)}</strong>
        </div>
        <button
          className="btn primary"
          disabled={items.length === 0}
          onClick={onCheckout}
        >
          Checkout
        </button>
      </div>
    </div>
  );
}
