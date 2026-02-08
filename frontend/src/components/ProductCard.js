import { formatZar } from "../api";

export default function ProductCard({ product, onAdd }) {
  return (
    <article className="card">
      <div className="card-media">
        {product.imageUrl ? (
          <img
            className="card-image"
            src={product.imageUrl}
            alt={product.name}
            loading="lazy"
          />
        ) : (
          <div className="card-image placeholder">No image</div>
        )}
      </div>
      <div className="card-body">
        <div className="card-title">
          <h3>{product.name}</h3>
          <span className="price">
            {formatZar(product.priceCents)} / {product.per}
          </span>
        </div>
        <p className="card-desc">{product.description}</p>
        <div className="badge-row">
          {(product.badges || []).slice(0, 3).map((b, i) => (
            <span key={`${b}-${i}`} className="badge">
              {b}
            </span>
          ))}
        </div>
        <button
          className="btn"
          disabled={!product.inStock}
          onClick={() => onAdd(product)}
        >
          {product.inStock ? "Add to cart" : "Out of stock"}
        </button>
      </div>
    </article>
  );
}
