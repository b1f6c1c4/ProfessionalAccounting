export default class Button {
    constructor(func, x, y, w, h) {
        if (!func) {
            this.func = undefined;
        } else if (typeof func === 'function') {
            this.func = func;
        } else if (typeof func === 'object') {
            this.func = () => func;
        }
        this.xlb = x;
        this.ylb = y;
        this.xub = x + w;
        this.yub = y + h;
    }
    check(p, store, ...args) {
        if (p.mouseX < this.xlb) return false;
        if (p.mouseX > this.xub) return false;
        if (p.mouseY < this.ylb) return false;
        if (p.mouseY > this.yub) return false;
        if (this.func)
            store.dispatch(this.func(...args));
        return true;
    }
}
