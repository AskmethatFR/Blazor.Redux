window.ReduxDevTools = {
    connection: null,

    init: function (dotNetHelper) {
        if (typeof window.__REDUX_DEVTOOLS_EXTENSION__ !== 'undefined') {
            this.connection = window.__REDUX_DEVTOOLS_EXTENSION__.connect({
                name: 'Blazor.Redux'
            });

            this.connection.subscribe((message) => {
                if (message.type === 'DISPATCH') {
                    dotNetHelper.invokeMethodAsync('OnDevToolsMessage', message);
                }
            });

            return true;
        }
        return false;
    },

    send: function (action, state) {
        if (this.connection) {
            this.connection.send(action, state);
        }
    },

    disconnect: function () {
        if (this.connection) {
            this.connection.unsubscribe();
            this.connection = null;
        }
    }
};