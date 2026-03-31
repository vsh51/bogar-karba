const test = require('node:test');
const assert = require('node:assert');
const fs = require('fs');
const path = require('path');

global.window = {};
global.document = {
    readyState: "complete",
    getElementById: () => null,
    createElement: () => ({ href: '', download: '', click: () => { } }),
    body: { appendChild: () => { }, removeChild: () => { } },
    addEventListener: () => { }
};

const mockStorage = new Map();
global.localStorage = {
    getItem: (key) => mockStorage.get(key) || null,
    setItem: (key, val) => mockStorage.set(key, val),
    clear: () => mockStorage.clear()
};

global.URL = {
    createObjectURL: () => "blob:test",
    revokeObjectURL: () => { }
};

global.Blob = class Blob {
    constructor(content, options) {
        this.content = content;
        this.options = options;
    }
};

global.alert = () => { };

const scriptPath = path.join(__dirname, '../../src/Web/wwwroot/js/checklist-sync.js');
const scriptContent = fs.readFileSync(scriptPath, 'utf8');
eval(scriptContent);

test('checklist-sync operations', async (t) => {

    t.beforeEach(() => {
        mockStorage.clear();
    });

    await t.test('importProgress should reject invalid JSON', () => {
        const sync = window.checklistProgressSync;
        const result = sync.importProgress("invalid json");
        assert.strictEqual(result, false);
        assert.strictEqual(mockStorage.has("bogarKarba.checklistProgress"), false);
    });

    await t.test('importProgress should reject wrong version or structure', () => {
        const sync = window.checklistProgressSync;
        const invalidData = JSON.stringify({ v: 2, checklists: {} });
        const result = sync.importProgress(invalidData);
        assert.strictEqual(result, false);

        const invalidData2 = JSON.stringify({ v: 1 });
        assert.strictEqual(sync.importProgress(invalidData2), false);
    });

    await t.test('importProgress should merge valid checklists data', () => {
        const sync = window.checklistProgressSync;


        mockStorage.set("bogarKarba.checklistProgress", JSON.stringify({
            v: 1,
            checklists: {
                "checklist-1": { completed: ["task-1"] }
            }
        }));

        const importedData = JSON.stringify({
            v: 1,
            checklists: {
                "checklist-2": { completed: ["task-2"] },
                "checklist-1": { completed: ["task-1", "task-3"] }
            }
        });

        const result = sync.importProgress(importedData);
        assert.strictEqual(result, true);

        const newData = JSON.parse(mockStorage.get("bogarKarba.checklistProgress"));
        assert.strictEqual(newData.v, 1);
        assert.deepStrictEqual(newData.checklists["checklist-1"].completed, ["task-1", "task-3"]);
        assert.deepStrictEqual(newData.checklists["checklist-2"].completed, ["task-2"]);
    });

    await t.test('exportProgress should trigger download with current localStorage data', () => {
        const sync = window.checklistProgressSync;
        mockStorage.set("bogarKarba.checklistProgress", JSON.stringify({
            v: 1,
            checklists: { "c1": { completed: ["t1"] } }
        }));

        let clickCalled = false;
        let originalCreateElement = global.document.createElement;
        global.document.createElement = (tag) => {
            if (tag === 'a') {
                return {
                    href: '',
                    download: '',
                    click: () => { clickCalled = true; }
                };
            }
            return originalCreateElement(tag);
        };

        sync.exportProgress();
        assert.strictEqual(clickCalled, true);

        global.document.createElement = originalCreateElement;
    });

});
