const path = require('path');

module.exports = {
    entry: './wwwroot/Mox/static/htmltable/js/src/HtmlTable.ts',
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: 'ts-loader',
                exclude: /node_modules/,
            },
        ],
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
    },
    output: {
        library: {
            name: 'HtmlTable',
            type: 'var'
        },
        filename: 'htmltable.js',
        path: path.resolve(__dirname, 'wwwroot/Mox/static/htmltable/js'),
    }
};