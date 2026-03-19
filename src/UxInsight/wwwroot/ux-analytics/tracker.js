(function () {
  "use strict";

  // Don't track backoffice or API routes
  if (
    location.pathname.startsWith("/umbraco") ||
    location.pathname.startsWith("/api/")
  )
    return;

  const API_URL = "/api/ux-analytics/track";
  const FLUSH_INTERVAL = 5000;
  const MAX_BATCH = 50;

  // Session management
  let sessionId = sessionStorage.getItem("ux-sid");
  if (!sessionId) {
    sessionId = crypto.randomUUID();
    sessionStorage.setItem("ux-sid", sessionId);
  }

  const eventQueue = [];
  const screenWidth = window.innerWidth;
  const screenHeight = window.innerHeight;

  function createEvent(type, extra) {
    return {
      eventType: type,
      pageUrl: location.pathname + location.search,
      referrer: document.referrer || null,
      timestamp: Date.now(),
      screenWidth,
      screenHeight,
      ...extra,
    };
  }

  function queueEvent(type, extra) {
    eventQueue.push(createEvent(type, extra));
    if (eventQueue.length >= MAX_BATCH) flush();
  }

  function flush() {
    if (eventQueue.length === 0) return;
    const batch = eventQueue.splice(0, MAX_BATCH);
    const payload = JSON.stringify({ sessionId, events: batch });

    if (navigator.sendBeacon) {
      navigator.sendBeacon(API_URL, new Blob([payload], { type: "application/json" }));
    } else {
      fetch(API_URL, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: payload,
        keepalive: true,
      }).catch(() => {});
    }
  }

  // ---- Pageview ----
  queueEvent("pageview");

  // ---- Scroll tracking ----
  let maxScroll = 0;
  let scrollTimer = null;
  function onScroll() {
    if (scrollTimer) return;
    scrollTimer = setTimeout(() => {
      scrollTimer = null;
      const scrollTop = window.scrollY || document.documentElement.scrollTop;
      const docHeight = Math.max(
        document.body.scrollHeight,
        document.documentElement.scrollHeight
      );
      const winHeight = window.innerHeight;
      const percent = docHeight <= winHeight ? 100 : Math.round((scrollTop / (docHeight - winHeight)) * 100);
      if (percent > maxScroll) maxScroll = percent;
    }, 500);
  }
  window.addEventListener("scroll", onScroll, { passive: true });

  // ---- Click tracking ----
  function getSelector(el) {
    if (!el || el === document.body || el === document.documentElement) return "body";
    const parts = [];
    let current = el;
    for (let i = 0; i < 4 && current && current !== document.body; i++) {
      let tag = current.tagName.toLowerCase();
      if (current.id) {
        parts.unshift("#" + current.id);
        break;
      }
      if (current.className && typeof current.className === "string") {
        const cls = current.className.trim().split(/\s+/).slice(0, 2).join(".");
        if (cls) tag += "." + cls;
      }
      parts.unshift(tag);
      current = current.parentElement;
    }
    return parts.join(" > ");
  }

  document.addEventListener("click", function (e) {
    const target = e.target;
    const selector = getSelector(target);
    const text = (target.textContent || "").trim().substring(0, 50);
    queueEvent("click", {
      elementSelector: selector,
      data: JSON.stringify({
        text,
        tag: target.tagName.toLowerCase(),
        x: Math.round((e.clientX / screenWidth) * 100),
        y: Math.round((e.clientY / screenHeight) * 100),
      }),
    });
  });

  // ---- Mouse heatmap (throttled, grid-bucketed) ----
  const mouseGrid = {};
  let mouseMoveTimer = null;
  document.addEventListener(
    "mousemove",
    function (e) {
      if (mouseMoveTimer) return;
      mouseMoveTimer = setTimeout(() => {
        mouseMoveTimer = null;
        const gx = Math.floor((e.clientX / screenWidth) * 10);
        const gy = Math.floor((e.clientY / screenHeight) * 10);
        const key = gx + "," + gy;
        mouseGrid[key] = (mouseGrid[key] || 0) + 1;
      }, 1000);
    },
    { passive: true }
  );

  // ---- Form interactions ----
  document.addEventListener("focusin", function (e) {
    const t = e.target;
    if (t.tagName === "INPUT" || t.tagName === "TEXTAREA" || t.tagName === "SELECT") {
      queueEvent("formfocus", { elementSelector: getSelector(t) });
    }
  });

  document.addEventListener("focusout", function (e) {
    const t = e.target;
    if (t.tagName === "INPUT" || t.tagName === "TEXTAREA" || t.tagName === "SELECT") {
      queueEvent("formblur", { elementSelector: getSelector(t) });
    }
  });

  // ---- Time on page & exit events ----
  const pageStart = performance.now();

  function sendExitEvents() {
    const seconds = Math.round((performance.now() - pageStart) / 1000);
    queueEvent("timeonpage", {
      data: JSON.stringify({ seconds }),
    });

    if (maxScroll > 0) {
      queueEvent("scroll", {
        data: JSON.stringify({ maxScroll }),
      });
    }

    if (Object.keys(mouseGrid).length > 0) {
      queueEvent("mousemove", {
        data: JSON.stringify({ grid: mouseGrid }),
      });
    }

    flush();
  }

  document.addEventListener("visibilitychange", function () {
    if (document.visibilityState === "hidden") sendExitEvents();
  });

  window.addEventListener("beforeunload", sendExitEvents);

  // Periodic flush
  setInterval(flush, FLUSH_INTERVAL);
})();
