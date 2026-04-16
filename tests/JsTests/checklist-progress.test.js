const test = require("node:test");
const assert = require("node:assert");
const fs = require("fs");
const path = require("path");

function setupEnvironment(options = {}) {
    const listenersByElement = new Map();

    const checklistId = options.checklistId || "checklist-1";
    const inputIds = options.inputIds || ["task-1", "task-2", "task-3"];
    const completedTaskIds = options.completedTaskIds || [];

    const progressPercent = { textContent: "" };
    const progressCount = { textContent: "" };
    const progressBar = { style: { width: "" } };
    const progressBarWrapAttributes = { "aria-valuenow": "0" };
    const progressBarWrap = {
        setAttribute(name, value) {
            progressBarWrapAttributes[name] = String(value);
        },
        getAttribute(name) {
            return progressBarWrapAttributes[name];
        }
    };

    const inputs = inputIds.map((taskId) => {
        const input = {
            checked: completedTaskIds.includes(taskId),
            getAttribute(name) {
                if (name === "data-task-id") {
                    return taskId;
                }
                return null;
            },
            addEventListener(eventName, handler) {
                listenersByElement.set(input, listenersByElement.get(input) || {});
                listenersByElement.get(input)[eventName] = handler;
            }
        };

        return input;
    });

    const page = {
        getAttribute(name) {
            if (name === "data-checklist-id") {
                return checklistId;
            }

            return null;
        },
        querySelectorAll(selector) {
            if (selector === "input.checklist-item-input[data-task-id]") {
                return inputs;
            }

            return [];
        },
        querySelector(selector) {
            if (selector === "[data-progress-percent]") {
                return progressPercent;
            }

            if (selector === "[data-progress-count]") {
                return progressCount;
            }

            if (selector === "[data-progress-bar]") {
                return progressBar;
            }

            if (selector === "[data-progress-bar-wrap]") {
                return progressBarWrap;
            }

            return null;
        }
    };

    global.window = {};

    const storage = new Map();
    if (completedTaskIds.length > 0) {
        storage.set(
            "bogarKarba.checklistProgress",
            JSON.stringify({ v: 1, checklists: { [checklistId]: { completed: completedTaskIds } } })
        );
    }

    global.localStorage = {
        getItem(key) {
            return storage.has(key) ? storage.get(key) : null;
        },
        setItem(key, value) {
            storage.set(key, value);
        },
        clear() {
            storage.clear();
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
        inputs,
        progressPercent,
        progressCount,
        progressBar,
        progressBarWrap,
        getChangeHandler(input) {
            const listeners = listenersByElement.get(input) || {};
            return listeners.change;
        }
    };
}

function loadScript() {
    const scriptPath = path.join(__dirname, "../../src/Web/wwwroot/js/checklist-progress.js");
    const scriptContent = fs.readFileSync(scriptPath, "utf8");
    eval(scriptContent);
}

test("checklist-progress renders and updates completion percentage", async (t) => {
    await t.test("renders initial percent/count/progress bar from localStorage", () => {
        const env = setupEnvironment({
            inputIds: ["task-1", "task-2", "task-3", "task-4"],
            completedTaskIds: ["task-2", "task-4"]
        });

        loadScript();

        assert.strictEqual(env.inputs[0].checked, false);
        assert.strictEqual(env.inputs[1].checked, true);
        assert.strictEqual(env.inputs[2].checked, false);
        assert.strictEqual(env.inputs[3].checked, true);

        assert.strictEqual(env.progressPercent.textContent, "50%");
        assert.strictEqual(env.progressCount.textContent, "2/4");
        assert.strictEqual(env.progressBar.style.width, "50%");
        assert.strictEqual(env.progressBarWrap.getAttribute("aria-valuenow"), "50");
    });

    await t.test("updates percent/count/progress bar when checkbox is toggled", () => {
        const env = setupEnvironment({
            inputIds: ["task-1", "task-2", "task-3"],
            completedTaskIds: ["task-1"]
        });

        loadScript();

        const targetInput = env.inputs[1];
        const onChange = env.getChangeHandler(targetInput);

        assert.ok(onChange);

        targetInput.checked = true;
        onChange();

        assert.strictEqual(targetInput.checked, true);
        assert.strictEqual(env.progressPercent.textContent, "67%");
        assert.strictEqual(env.progressCount.textContent, "2/3");
        assert.strictEqual(env.progressBar.style.width, "67%");
        assert.strictEqual(env.progressBarWrap.getAttribute("aria-valuenow"), "67");
    });
});
