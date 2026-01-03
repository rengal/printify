// Documentation page JavaScript
// Handles sidebar toggle, theme switching, and navigation highlighting

console.info('docs.js loaded', new Date().toISOString());

// Theme Functions (shared with main app)
function toggleTheme() {
    const html = document.documentElement;
    const current = html.getAttribute('data-theme');
    const next = current === 'dark' ? 'light' : 'dark';
    html.setAttribute('data-theme', next);
    localStorage.setItem('theme', next);
    updateThemeIcon(next);
}

function updateThemeIcon(theme) {
    const darkIcon = document.getElementById('themeIconDark');
    const lightIcon = document.getElementById('themeIconLight');

    if (!darkIcon || !lightIcon) return;

    if (theme === 'dark') {
        darkIcon.style.display = 'block';
        lightIcon.style.display = 'none';
    } else {
        darkIcon.style.display = 'none';
        lightIcon.style.display = 'block';
    }
}

function initTheme() {
    const html = document.documentElement;
    const saved = localStorage.getItem('theme') || 'dark';
    html.setAttribute('data-theme', saved);
    updateThemeIcon(saved);
}

// Sidebar Toggle Functions
function toggleDocsSidebar() {
    const container = document.querySelector('.docs-container');
    container.classList.toggle('sidebar-minimized');

    const isMinimized = container.classList.contains('sidebar-minimized');
    localStorage.setItem('docsSidebarMinimized', isMinimized);
}

function initSidebar() {
    const sidebarMinimized = localStorage.getItem('docsSidebarMinimized') === 'true';
    if (sidebarMinimized) {
        document.querySelector('.docs-container').classList.add('sidebar-minimized');
    }
}

// Highlight current page in navigation
function highlightCurrentPage() {
    const currentPath = window.location.pathname;
    const navItems = document.querySelectorAll('.docs-nav-item');

    navItems.forEach(item => {
        const href = item.getAttribute('href');
        if (href && currentPath.includes(href)) {
            item.classList.add('active');
        } else {
            item.classList.remove('active');
        }
    });
}

// Code Tabs Functions
function switchCodeTab(tabsContainer, tabName) {
    const buttons = tabsContainer.querySelectorAll('.code-tab-btn');
    const panels = tabsContainer.querySelectorAll('.code-tab-panel');

    buttons.forEach(btn => {
        if (btn.dataset.tab === tabName) {
            btn.classList.add('active');
        } else {
            btn.classList.remove('active');
        }
    });

    panels.forEach(panel => {
        if (panel.dataset.tab === tabName) {
            panel.classList.add('active');
        } else {
            panel.classList.remove('active');
        }
    });

    // Save preference
    localStorage.setItem('preferredCodeTab', tabName);
}

function initCodeTabs() {
    const allTabContainers = document.querySelectorAll('.code-tabs');
    const preferredTab = localStorage.getItem('preferredCodeTab') || 'csharp';

    allTabContainers.forEach(container => {
        const buttons = container.querySelectorAll('.code-tab-btn');

        buttons.forEach(btn => {
            btn.addEventListener('click', () => {
                switchCodeTab(container, btn.dataset.tab);
            });
        });

        // Set initial active tab (prefer saved preference if available)
        const tabToActivate = container.querySelector(`[data-tab="${preferredTab}"]`) ? preferredTab : buttons[0]?.dataset.tab;
        if (tabToActivate) {
            switchCodeTab(container, tabToActivate);
        }
    });
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    initTheme();
    initSidebar();
    highlightCurrentPage();
    initCodeTabs();
});
