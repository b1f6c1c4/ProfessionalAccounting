export default class Button {
    constructor(x, y, w, h) {
        this.xlb = x;
        this.ylb = y;
        this.xub = x + w;
        this.yub = y + h;
    }
    check(p) {
        if (p.mouseX < this.xlb) return false;
        if (p.mouseX > this.xub) return false;
        if (p.mouseY < this.ylb) return false;
        if (p.mouseY > this.yub) return false;
        return true;
    }
    dispatch(p, store, action) {
        if (this.check(p, store)) {
            store.dispatch(action);
            return true;
        }
        return false;
    }
}
