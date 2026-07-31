import { useState, useEffect, useRef } from "react";
import { api } from "./api";
import "./App.css";

export default function App() {
  const [user, setUser] = useState(null);
  const [products, setProducts] = useState([]);
  const [cart, setCart] = useState({});
  const [order, setOrder] = useState(null);
  const [payment, setPayment] = useState(null);
  const [status, setStatus] = useState("");
  const [authForm, setAuthForm] = useState({ name: "", email: "", password: "" });
  const [mode, setMode] = useState("login");
  const statusRef = useRef(null);

useEffect(() => {
  api.getProducts().then(setProducts).catch((e) => setStatus(e.message));
}, []);

  const scrollToStatus = () => {
    setTimeout(() => {
      statusRef.current?.scrollIntoView({ behavior: "smooth", block: "center" });
    }, 100);
  };

  const handleAuth = async (e) => {
    e.preventDefault();
    setStatus("");
    try {
      const result =
        mode === "login"
          ? await api.login(authForm.email, authForm.password)
          : await api.register(authForm.name, authForm.email, authForm.password);
      setUser(result);
      setStatus(mode === "login" ? "Logged in." : "Registered — you can now log in.");
      scrollToStatus();
    } catch (err) {
      setStatus(err.message);
      scrollToStatus();
    }
  };

  const addToCart = (productId) => {
    setCart((prev) => ({ ...prev, [productId]: (prev[productId] || 0) + 1 }));
  };

  const removeFromCart = (productId) => {
    setCart((prev) => {
      const newCart = { ...prev };
      if (newCart[productId] > 1) {
        newCart[productId] -= 1;
      } else {
        delete newCart[productId];
      }
      return newCart;
    });
  };

  const placeOrder = async () => {
    setStatus("");
    const items = Object.entries(cart).map(([productId, quantity]) => ({
      productId: Number(productId),
      quantity,
    }));
    if (items.length === 0) {
      setStatus("Your cart is empty.");
      scrollToStatus();
      return;
    }
    try {
      const result = await api.createOrder(items);
      setOrder(result.orderId);
      setStatus(`Order #${result.orderId} created.`);
      scrollToStatus();
    } catch (err) {
      setStatus(err.message);
      scrollToStatus();
    }
  };

  const startCheckout = async (provider) => {
    setStatus("");
    try {
      const result = await api.checkout(order, provider);
      if (result.redirectUrl) {
        window.location.href = result.redirectUrl;
        return;
      }
      setPayment(result);
      setStatus(`Checkout started via ${provider}. Transaction: ${result.transactionId}`);
      scrollToStatus();
    } catch (err) {
      setStatus(err.message);
      scrollToStatus();
    }
  };

  const confirmPayment = async () => {
    setStatus("");
    try {
      const result = await api.confirmPayment(payment.provider, payment.transactionId);
      const msg = result.success
        ? `Payment confirmed. Order status: ${result.orderStatus}.`
        : `Payment not yet completed (status: ${result.paymentStatus}).`;
      setStatus(msg);
      scrollToStatus();
    } catch (err) {
      setStatus(err.message);
      scrollToStatus();
    }
  };

  const logout = () => {
    setUser(null);
    setCart({});
    setOrder(null);
    setPayment(null);
    setStatus("Logged out.");
    scrollToStatus();
    setAuthForm({ name: "", email: "", password: "" });
  };

  const getCartTotal = () => {
    let total = 0;
    Object.entries(cart).forEach(([id, qty]) => {
      const product = products.find((p) => p.id === Number(id));
      if (product) total += product.price * qty;
    });
    return total.toFixed(2);
  };

  const cartItemCount = Object.values(cart).reduce((sum, qty) => sum + qty, 0);

  return (
    <div className="app">
      <header>
        <h1>🛒 Shop</h1>
        {user && (
          <div className="header-right">
            <span className="user-badge">👤 {user.name}</span>
            <button className="logout-btn" onClick={logout}>Logout</button>
          </div>
        )}
      </header>

      {status && <p className="status" ref={statusRef}>{status}</p>}

      {/* 1. SHOW LOGIN/REGISTER – ONLY when user is NOT logged in */}
      {!user && (
        <section className="auth-section">
          <div className="tabs">
            <button className={mode === "login" ? "active" : ""} onClick={() => setMode("login")}>
              Log in
            </button>
            <button className={mode === "register" ? "active" : ""} onClick={() => setMode("register")}>
              Register
            </button>
          </div>
          <form onSubmit={handleAuth}>
            {mode === "register" && (
              <input
                placeholder="Name"
                value={authForm.name}
                onChange={(e) => setAuthForm({ ...authForm, name: e.target.value })}
                required
              />
            )}
            <input
              placeholder="Email"
              type="email"
              value={authForm.email}
              onChange={(e) => setAuthForm({ ...authForm, email: e.target.value })}
              required
            />
            <input
              placeholder="Password"
              type="password"
              value={authForm.password}
              onChange={(e) => setAuthForm({ ...authForm, password: e.target.value })}
              required
            />
            <button type="submit">{mode === "login" ? "Log in" : "Create account"}</button>
          </form>
        </section>
      )}

      {/* 2. SHOW PRODUCTS + CART – ONLY when user IS logged in */}
      {user && (
        <div className="main-layout">
          <section className="product-section">
            <h2>Products</h2>
            <div className="product-grid">
              {products.map((p) => (
                <div className="product" key={p.id}>
                  <h3>{p.name}</h3>
                  <p>{p.description}</p>
                  <p className="price">${p.price.toFixed(2)}</p>
                  <p className="stock">Stock: {p.stock}</p>
                  <button onClick={() => addToCart(p.id)}>
                    Add to cart {cart[p.id] ? `(${cart[p.id]})` : ""}
                  </button>
                </div>
              ))}
            </div>
          </section>

          <aside className="cart-section">
            <h2>🛍️ Your Cart</h2>
            {cartItemCount === 0 ? (
              <p className="empty-cart">Your cart is empty. Add some products!</p>
            ) : (
              <>
                <ul className="cart-items">
                  {Object.entries(cart).map(([id, qty]) => {
                    const product = products.find((p) => p.id === Number(id));
                    return product ? (
                      <li key={id}>
                        <span>{product.name} × {qty}</span>
                        <span>${(product.price * qty).toFixed(2)}</span>
                        <button onClick={() => removeFromCart(Number(id))} className="remove-btn">−</button>
                        <button onClick={() => addToCart(Number(id))} className="add-btn">+</button>
                      </li>
                    ) : null;
                  })}
                </ul>
                <div className="cart-total">
                  <strong>Total: ${getCartTotal()}</strong>
                </div>
                <button className="place-order-btn" onClick={placeOrder}>Place Order</button>
              </>
            )}

            {order && !payment && (
              <div className="payment-options">
                <p>Order #{order} created. Choose payment:</p>
                <button onClick={() => startCheckout("stripe")}>Pay with Stripe</button>
                <button onClick={() => startCheckout("bkash")}>Pay with bKash</button>
              </div>
            )}

            {payment && (
              <div className="payment-options">
                <p>Transaction: {payment.transactionId}</p>
                <button onClick={confirmPayment}>Confirm payment</button>
              </div>
            )}
          </aside>
        </div>
      )}
    </div>
  );
}