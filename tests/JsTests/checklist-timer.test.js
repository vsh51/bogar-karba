const test = require("node:test");
const assert = require("node:assert");
const fs = require("fs");
const path = require("path");

function setupEnvironment(options = {}) {
    const checklistId = options.checklistId || "checklist-1";
    const storage = new Map();
    const listeners = new Map();

    const display = { textContent: "" };
    const state = { textContent: "" };

    function createButton() {
        const btn = {
            disabled: false,
            addEventListener(eventName, handler) {
                listeners.set(btn, listeners.get(btn) || {});
                listeners.get(btn)[eventName] = handler;
            }
        };
        return btn;
    }

    const startBtn = createButton();
    const stopBtn = createButton();
    const restartBtn = createButton();

    const page = {
        getAttribute(name) {
            if (name === "data-checklist-id") {
                return checklistId;
            }
            return null;
        },
        querySelector(selector) {
            if (selector === "[data-timer-display]") {
                return display;
            }

            if (selector === "[data-timer-state]") {
                return state;
            }

            if (selector === "[data-timer-start]") {
                return startBtn;
            }

            if (selector === "[data-timer-stop]") {
                return stopBtn;
            }

            if (selector === "[data-timer-restart]") {
                return restartBtn;
            }

            return null;
        }
    };

    global.window = {
        setInterval(fn) {
            window.__intervalHandler = fn;
            return 1;
        },
        clearInterval() {},
        addEventListener() {}
    };

    global.localStorage = {
        getItem(key) {
            return storage.has(key) ? storage.get(key) : null;
        },
        setItem(key, value) {
            storage.set(key, value);
        }
    };

    global.document = {
        readyState: "complete",
        querySelector(selector) {
            if (selector === ".checklist-page[data-checklist-id]") {
                return page;
            }
            return null;
        },
        addEventListener() {}
    };

    return {
        display,
        state,
        startBtn,
        stopBtn,
        restartBtn,
        getClickHandler(element) {
            const byEvent = listeners.get(element) || {};
            return byEvent.click;
        },
        getStorageValue(key) {
            return storage.get(key);
        }
    };
}

function loadScript() {
    const scriptPath = path.join(__dirname, "../../src/Web/wwwroot/js/checklist-timer.js");
    const scriptContent = fs.readFileSync(scriptPath, "utf8");
    eval(scriptContent);
}

test("checklist-timer renders default stopped state", () => {
    const env = setupEnvironment();

    loadScript();

    assert.strictEqual(env.display.textContent, "00:00:00");
    assert.strictEqual(env.state.textContent, "Stopped");
    assert.strictEqual(env.startBtn.disabled, false);
    assert.strictEqual(env.stopBtn.disabled, true);
});

test("checklist-timer supports start stop and restart", () => {
    let now = 1_000;
    const originalNow = Date.now;
    Date.now = () => now;

    const env = setupEnvironment();
    loadScript();

    const onStart = env.getClickHandler(env.startBtn);
    const onStop = env.getClickHandler(env.stopBtn);
    const onRestart = env.getClickHandler(env.restartBtn);

    assert.ok(onStart);
    assert.ok(onStop);
    assert.ok(onRestart);

    onStart();
    assert.strictEqual(env.state.textContent, "Running");
    assert.strictEqual(env.startBtn.disabled, true);
    assert.strictEqual(env.stopBtn.disabled, false);

    now += 5_000;
    onStop();
    assert.strictEqual(env.display.textContent, "00:00:05");
    assert.strictEqual(env.state.textContent, "Stopped");

    const storedRaw = env.getStorageValue("bogarKarba.checklistTimer.checklist-1");
    assert.ok(storedRaw);
    assert.strictEqual(storedRaw, "5000;0;");

    onRestart();
    assert.strictEqual(env.display.textContent, "00:00:00");
    assert.strictEqual(env.state.textContent, "Stopped");

    Date.now = originalNow;
});
