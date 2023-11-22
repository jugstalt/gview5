(function () {
    let isResizing = false;

    //document.addEventListener('DOMContentLoaded', function () {
    //    document.querySelector('.tree-content-splitter').addEventListener('mousedown', function (e) {
    //        isResizing = true;
    //    });
    //});

    //let splitterElement = document.querySelector('.tree-content-splitter');

    window.addEventListener('mousedown', function (e) {
        //if (e.target === splitterElement) {
        if (e.target.classList.contains('tree-content-splitter')) {
            isResizing = true;
        }
    });

    window.addEventListener('mousemove', function (e) {
        if (!isResizing) return;
        let container = document.querySelector('.dataexplorer-main');

        let leftWidth = e.clientX - container.offsetLeft;

        leftWidth = Math.max(leftWidth, 10);
        leftWidth = Math.min(leftWidth, container.clientWidth - 10);

        document.querySelector('.tree').style.width = leftWidth + 'px';
        document.querySelector('.tree-content-splitter').style.left = (leftWidth) + 'px';
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