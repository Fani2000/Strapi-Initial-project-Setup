import { formatZar } from "../api";
import { Link } from "react-router-dom";

export default function ProductCard({ product, onAdd }) {
  return (
    <article className="card">
      <Link to={`/products/${product.slug}`} className="card-link">
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
      </Link>
      <div className="card-body">
        <div className="price-chip">
          from {formatZar(product.priceCents)} / {product.per}
        </div>
        <div className="card-title">
          <h3>
            <Link to={`/products/${product.slug}`} className="card-link">
              {product.name}
            </Link>
          </h3>
        </div>
        <p className="card-desc">{product.description}</p>
        <div className="badge-row">
          {(product.badges || []).slice(0, 3).map((b, i) => (
            <span key={`${b}-${i}`} className="badge">
              {b}
            </span>
          ))}
        </div>
        <div className="card-footer">
          <span className={`stock ${product.inStock ? "in" : "out"}`}>
            {product.inStock ? "In stock" : "Out of stock"}
          </span>
          <button
            className="add-btn"
            disabled={!product.inStock}
            onClick={() => onAdd(product)}
            aria-label={`Add ${product.name} to cart`}
          >
            +
          </button>
        </div>
      </div>
    </article>
  );
}
