// Zero-dependency motion for the landing page.
// Scroll reveals (IntersectionObserver), pointer-tracked 3D tilt, count-up.
// Wired from Home.razor.cs OnAfterRenderAsync. Respects prefers-reduced-motion.

let observers = [];
let cleanups = [];

const reduced = () =>
    window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

// Formats a count-up value using the element's data-* options.
// Opt-in thousands grouping via data-group="true" (off by default, so
// existing landing counters are byte-for-byte unchanged).
function formatNum(el, value) {
    const decimals = parseInt(el.dataset.decimals || '0', 10);
    const prefix = el.dataset.prefix || '';
    const suffix = el.dataset.suffix || '';
    const body = el.dataset.group === 'true'
        ? value.toLocaleString(undefined, { minimumFractionDigits: decimals, maximumFractionDigits: decimals })
        : value.toFixed(decimals);
    return prefix + body + suffix;
}

export function init(root) {
    const scope = root && root.querySelectorAll ? root : document;
    // Marks the root so reveal styles only hide content when JS is running
    // (no-JS / headless renders keep [data-anim] visible by default).
    if (root && root.classList) root.classList.add('js-motion');
    reveal(scope);
    tilt(scope);
}

export function dispose() {
    observers.forEach(o => o.disconnect());
    cleanups.forEach(fn => fn());
    observers = [];
    cleanups = [];
}

/* ---------- Scroll reveal + count-up trigger ---------- */
function reveal(scope) {
    const items = scope.querySelectorAll('[data-anim]');

    if (reduced()) {
        items.forEach(el => el.classList.add('is-in'));
        scope.querySelectorAll('[data-count]').forEach(setFinal);
        return;
    }

    const io = new IntersectionObserver((entries, obs) => {
        for (const entry of entries) {
            if (!entry.isIntersecting) continue;
            const el = entry.target;
            el.classList.add('is-in');
            el.querySelectorAll('[data-count]').forEach(countUp);
            obs.unobserve(el);
        }
    }, { threshold: 0.2, rootMargin: '0px 0px -8% 0px' });

    items.forEach(el => io.observe(el));
    observers.push(io);
}

function countUp(el) {
    if (reduced()) { setFinal(el); return; }

    const target = parseFloat(el.dataset.count);
    if (Number.isNaN(target)) return;

    const duration = 1100;
    const ease = t => 1 - Math.pow(1 - t, 3);
    let start;

    const step = ts => {
        if (start === undefined) start = ts;
        const p = Math.min((ts - start) / duration, 1);
        el.textContent = formatNum(el, target * ease(p));
        if (p < 1) requestAnimationFrame(step);
        else el.textContent = formatNum(el, target);
    };
    requestAnimationFrame(step);
}

function setFinal(el) {
    const target = parseFloat(el.dataset.count);
    if (Number.isNaN(target)) return;
    el.textContent = formatNum(el, target);
}

/* ---------- Pointer-tracked 3D tilt ---------- */
function tilt(scope) {
    if (reduced()) return;

    const plate = scope.querySelector
        ? scope.querySelector('#smoPlate')
        : document.getElementById('smoPlate');
    if (!plate) return;

    const MAX = 8; // degrees
    let rect = null;
    let raf = 0;

    const onEnter = () => {
        rect = plate.getBoundingClientRect();
        plate.classList.add('is-tilting');
    };

    const onMove = e => {
        if (!rect) rect = plate.getBoundingClientRect();
        if (raf) return;
        raf = requestAnimationFrame(() => {
            raf = 0;
            const px = (e.clientX - rect.left) / rect.width;
            const py = (e.clientY - rect.top) / rect.height;
            plate.style.setProperty('--ry', ((px - 0.5) * MAX * 2).toFixed(2) + 'deg');
            plate.style.setProperty('--rx', ((0.5 - py) * MAX * 2).toFixed(2) + 'deg');
            plate.style.setProperty('--gx', (px * 100).toFixed(1) + '%');
            plate.style.setProperty('--gy', (py * 100).toFixed(1) + '%');
        });
    };

    const onLeave = () => {
        if (raf) { cancelAnimationFrame(raf); raf = 0; }
        plate.classList.remove('is-tilting');
        plate.style.setProperty('--ry', '0deg');
        plate.style.setProperty('--rx', '0deg');
        plate.style.setProperty('--gx', '50%');
        plate.style.setProperty('--gy', '0%');
        rect = null;
    };

    plate.addEventListener('pointerenter', onEnter);
    plate.addEventListener('pointermove', onMove);
    plate.addEventListener('pointerleave', onLeave);

    cleanups.push(() => {
        plate.removeEventListener('pointerenter', onEnter);
        plate.removeEventListener('pointermove', onMove);
        plate.removeEventListener('pointerleave', onLeave);
    });
}
