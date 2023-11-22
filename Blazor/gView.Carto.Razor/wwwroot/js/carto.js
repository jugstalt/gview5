(function () {
    let isResizing = false;
    //document.querySelector('.toc-content-splitter').addEventListener('mousedown', function (e) {
    //    isResizing = true;
    //});

    window.addEventListener('mousedown', function (e) {
        if (e.target.classList.contains('toc-content-splitter')) {
            isResizing = true;
        }
    });

    window.addEventListener('mousemove', function (e) {
        if (!isResizing) return;
        let container = document.querySelector('.carto-main');

        let leftWidth = e.clientX - container.offsetLeft;

        leftWidth = Math.max(leftWidth, 10);
        leftWidth = Math.min(leftWidth, container.clientWidth - 10);

        document.querySelector('.toc').style.width = leftWidth + 'px';
        document.querySelector('.toc-content-splitter').style.left = (leftWidth) + 'px';
        document.querySelector('.content').style.left = (leftWidth + 5) + 'px';
    });

    window.addEventListener('mouseup', function (e) {
        if (isResizing === true) {
            isResizing = false;

            if (window.gViewLeaflet) {
                window.gViewLeaflet.resizeAllMaps();
            }
        }
    });
}());