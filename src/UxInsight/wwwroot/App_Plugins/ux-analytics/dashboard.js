import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import {
  html,
  css,
  LitElement,
  nothing,
} from "@umbraco-cms/backoffice/external/lit";

const API_BASE = "/api/ux-analytics/dashboard";

export default class UxAnalyticsDashboard extends UmbElementMixin(LitElement) {
  static properties = {
    _stats: { state: true },
    _analysis: { state: true },
    _history: { state: true },
    _loading: { state: true },
    _error: { state: true },
    _activeTab: { state: true },
  };

  #authContext;

  constructor() {
    super();
    this._stats = null;
    this._analysis = null;
    this._history = [];
    this._loading = false;
    this._error = null;
    this._activeTab = "bottlenecks";

    this.consumeContext(UMB_AUTH_CONTEXT, (authContext) => {
      this.#authContext = authContext;
      this._loadData();
    });
  }

  async _getAuthHeaders() {
    const headers = { "Content-Type": "application/json" };
    if (this.#authContext) {
      const config = this.#authContext.getOpenApiConfiguration();
      const token =
        typeof config.token === "function"
          ? await config.token()
          : config.token;
      if (token) {
        headers["Authorization"] = `Bearer ${token}`;
      }
    }
    return headers;
  }

  async _fetchApi(path, method = "GET") {
    const headers = await this._getAuthHeaders();
    const resp = await fetch(`${API_BASE}${path}`, {
      method,
      headers,
    });
    if (!resp.ok) {
      if (resp.status === 404) return null;
      throw new Error(`API error: ${resp.status}`);
    }
    return resp.json();
  }

  async _loadData() {
    if (!this.#authContext) return;
    try {
      const [stats, analysis, history] = await Promise.all([
        this._fetchApi("/summary"),
        this._fetchApi("/analysis/latest").catch(() => null),
        this._fetchApi("/analysis/history"),
      ]);
      this._stats = stats;
      this._analysis = analysis;
      this._history = history || [];
    } catch (e) {
      this._error = e.message;
    }
  }

  async _runAnalysis() {
    this._loading = true;
    this._error = null;
    try {
      this._analysis = await this._fetchApi("/analysis/run", "POST");
      this._history = (await this._fetchApi("/analysis/history")) || [];
    } catch (e) {
      this._error = "Analysis failed: " + e.message;
    } finally {
      this._loading = false;
    }
  }

  _parseJson(str) {
    try {
      return JSON.parse(str || "[]");
    } catch {
      return [];
    }
  }

  _formatDate(dateStr) {
    if (!dateStr) return "N/A";
    return new Date(dateStr).toLocaleString();
  }

  static styles = css`
    :host {
      display: block;
      padding: 20px;
    }
    .stats-bar {
      display: flex;
      gap: 20px;
      margin-bottom: 24px;
      flex-wrap: wrap;
    }
    .stat-card {
      background: var(--uui-color-surface-alt);
      border: 1px solid var(--uui-color-border);
      border-radius: 6px;
      padding: 16px 24px;
      min-width: 150px;
    }
    .stat-value {
      font-size: 28px;
      font-weight: bold;
      color: var(--uui-color-interactive);
    }
    .stat-label {
      font-size: 13px;
      color: var(--uui-color-text-alt);
      margin-top: 4px;
    }
    .tabs {
      display: flex;
      gap: 4px;
      margin-bottom: 16px;
      border-bottom: 1px solid var(--uui-color-border);
      padding-bottom: 0;
    }
    .tab {
      padding: 8px 16px;
      cursor: pointer;
      border: none;
      background: none;
      font-size: 14px;
      color: var(--uui-color-text-alt);
      border-bottom: 2px solid transparent;
      margin-bottom: -1px;
    }
    .tab.active {
      color: var(--uui-color-interactive);
      border-bottom-color: var(--uui-color-interactive);
      font-weight: 600;
    }
    .tab:hover {
      color: var(--uui-color-interactive-emphasis);
    }
    .panel {
      background: var(--uui-color-surface);
      border: 1px solid var(--uui-color-border);
      border-radius: 6px;
      padding: 20px;
      margin-bottom: 20px;
    }
    .item-card {
      border: 1px solid var(--uui-color-border);
      border-radius: 4px;
      padding: 12px 16px;
      margin-bottom: 10px;
      background: var(--uui-color-surface-alt);
    }
    .item-card h4 {
      margin: 0 0 6px 0;
      font-size: 14px;
    }
    .item-card p {
      margin: 4px 0;
      font-size: 13px;
      color: var(--uui-color-text-alt);
    }
    .severity-high {
      border-left: 3px solid #e74c3c;
    }
    .severity-medium {
      border-left: 3px solid #f39c12;
    }
    .severity-low {
      border-left: 3px solid #27ae60;
    }
    .badge {
      display: inline-block;
      padding: 2px 8px;
      border-radius: 10px;
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
    }
    .badge-high {
      background: #fde8e8;
      color: #e74c3c;
    }
    .badge-medium {
      background: #fef3e2;
      color: #f39c12;
    }
    .badge-low {
      background: #e8f8f0;
      color: #27ae60;
    }
    .actions-bar {
      display: flex;
      gap: 12px;
      align-items: center;
      margin-bottom: 24px;
    }
    .history-list {
      margin-top: 16px;
    }
    .history-item {
      display: flex;
      justify-content: space-between;
      padding: 8px 12px;
      border-bottom: 1px solid var(--uui-color-border);
      font-size: 13px;
      cursor: pointer;
    }
    .history-item:hover {
      background: var(--uui-color-surface-alt);
    }
    .error {
      color: #e74c3c;
      padding: 12px;
      background: #fde8e8;
      border-radius: 4px;
      margin-bottom: 16px;
    }
    .meta-info {
      font-size: 12px;
      color: var(--uui-color-text-alt);
      margin-bottom: 16px;
    }
  `;

  render() {
    return html`
      <uui-box headline="UX Analytics - AI-Powered Behavior Analysis">
        ${this._error ? html`<div class="error">${this._error}</div>` : nothing}
        ${this._renderStats()} ${this._renderActions()}
        ${this._analysis ? this._renderAnalysis() : this._renderEmpty()}
        ${this._renderHistory()}
      </uui-box>
    `;
  }

  _renderStats() {
    const s = this._stats;
    if (!s) return html`<p>Loading stats...</p>`;
    return html`
      <div class="stats-bar">
        <div class="stat-card">
          <div class="stat-value">${s.totalSessions || 0}</div>
          <div class="stat-label">Total Sessions</div>
        </div>
        <div class="stat-card">
          <div class="stat-value">${s.totalEvents || 0}</div>
          <div class="stat-label">Total Events</div>
        </div>
        <div class="stat-card">
          <div class="stat-value">${this._formatDate(s.firstEvent)}</div>
          <div class="stat-label">First Event</div>
        </div>
        <div class="stat-card">
          <div class="stat-value">${this._formatDate(s.lastEvent)}</div>
          <div class="stat-label">Last Event</div>
        </div>
      </div>
    `;
  }

  _renderActions() {
    return html`
      <div class="actions-bar">
        <uui-button
          look="primary"
          label="Run AI Analysis"
          ?disabled=${this._loading}
          @click=${this._runAnalysis}
        >
          ${this._loading ? "Analyzing..." : "Run AI Analysis"}
        </uui-button>
        <uui-button look="secondary" label="Refresh" @click=${this._loadData}>
          Refresh Data
        </uui-button>
      </div>
    `;
  }

  _renderEmpty() {
    return html`
      <div class="panel">
        <p>
          No analysis results yet. Click <strong>Run AI Analysis</strong> to
          analyze collected behavior data with Claude AI.
        </p>
        <p style="font-size: 13px; color: var(--uui-color-text-alt);">
          Make sure you have some tracking data first by visiting your website's
          frontend pages.
        </p>
      </div>
    `;
  }

  _renderAnalysis() {
    const a = this._analysis;
    const bottlenecks = this._parseJson(a.conversionBottlenecks);
    const suggestions = this._parseJson(a.uxSuggestions);
    const stuckPoints = this._parseJson(a.stuckPoints);
    const heatmap = this._parseJson(a.heatmapInsights);

    return html`
      <div class="meta-info">
        Analysis from ${this._formatDate(a.createdAt)} | Period:
        ${this._formatDate(a.analysisPeriodStart)} -
        ${this._formatDate(a.analysisPeriodEnd)} | ${a.totalSessions} sessions,
        ${a.totalEvents} events
      </div>

      <div class="tabs">
        <button
          class="tab ${this._activeTab === "bottlenecks" ? "active" : ""}"
          @click=${() => (this._activeTab = "bottlenecks")}
        >
          Conversion Bottlenecks (${bottlenecks.length})
        </button>
        <button
          class="tab ${this._activeTab === "suggestions" ? "active" : ""}"
          @click=${() => (this._activeTab = "suggestions")}
        >
          UX Suggestions (${suggestions.length})
        </button>
        <button
          class="tab ${this._activeTab === "stuck" ? "active" : ""}"
          @click=${() => (this._activeTab = "stuck")}
        >
          Stuck Points (${stuckPoints.length})
        </button>
        <button
          class="tab ${this._activeTab === "heatmap" ? "active" : ""}"
          @click=${() => (this._activeTab = "heatmap")}
        >
          Heatmap Insights (${heatmap.length})
        </button>
      </div>

      <div class="panel">
        ${this._activeTab === "bottlenecks"
          ? this._renderBottlenecks(bottlenecks)
          : nothing}
        ${this._activeTab === "suggestions"
          ? this._renderSuggestions(suggestions)
          : nothing}
        ${this._activeTab === "stuck"
          ? this._renderStuckPoints(stuckPoints)
          : nothing}
        ${this._activeTab === "heatmap"
          ? this._renderHeatmap(heatmap)
          : nothing}
      </div>
    `;
  }

  _renderBottlenecks(items) {
    if (!items.length)
      return html`<p>No conversion bottlenecks identified.</p>`;
    return html`
      ${items.map(
        (item) => html`
          <div class="item-card severity-${item.severity || "low"}">
            <h4>
              ${item.issue}
              <span class="badge badge-${item.severity || "low"}"
                >${item.severity || "N/A"}</span
              >
            </h4>
            <p><strong>Page:</strong> ${item.page || "N/A"}</p>
            <p>${item.recommendation}</p>
          </div>
        `
      )}
    `;
  }

  _renderSuggestions(items) {
    if (!items.length) return html`<p>No suggestions available.</p>`;
    return html`
      ${items.map(
        (item) => html`
          <div class="item-card">
            <h4>${item.area}</h4>
            <p>${item.suggestion}</p>
            <p>
              <span class="badge badge-${item.impact || "low"}"
                >Impact: ${item.impact || "N/A"}</span
              >
              &nbsp;
              <span class="badge badge-${item.effort || "low"}"
                >Effort: ${item.effort || "N/A"}</span
              >
            </p>
          </div>
        `
      )}
    `;
  }

  _renderStuckPoints(items) {
    if (!items.length) return html`<p>No stuck points identified.</p>`;
    return html`
      ${items.map(
        (item) => html`
          <div class="item-card">
            <h4>${item.page}</h4>
            <p><strong>Indicator:</strong> ${item.indicator}</p>
            <p>${item.description}</p>
          </div>
        `
      )}
    `;
  }

  _renderHeatmap(items) {
    if (!items.length) return html`<p>No heatmap insights available.</p>`;
    return html`
      ${items.map(
        (item) => html`
          <div class="item-card">
            <h4>${item.page}</h4>
            <p><strong>Observation:</strong> ${item.observation}</p>
            <p><strong>Recommendation:</strong> ${item.recommendation}</p>
          </div>
        `
      )}
    `;
  }

  _renderHistory() {
    if (!this._history || !this._history.length) return nothing;
    return html`
      <div style="margin-top: 24px;">
        <h3>Analysis History</h3>
        <div class="history-list">
          ${this._history.map(
            (h) => html`
              <div class="history-item" @click=${() => (this._analysis = h)}>
                <span>${this._formatDate(h.createdAt)}</span>
                <span>${h.totalSessions} sessions, ${h.totalEvents} events</span>
              </div>
            `
          )}
        </div>
      </div>
    `;
  }
}

customElements.define("ux-analytics-dashboard", UxAnalyticsDashboard);
