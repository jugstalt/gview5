window.gview_base = {
    setCursor: function (cursorStyle) {
        if (cursorStyle === 'wait') {
            document.body.classList.add('gview-razor-wait-cursor');
        } else {
            document.body.classList.remove('gview-razor-wait-cursor');
        }
    }
};