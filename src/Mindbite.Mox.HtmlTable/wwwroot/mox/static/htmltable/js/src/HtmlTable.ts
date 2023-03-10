import Chart, { ChartConfiguration, ChartData, ChartType } from 'Chart.js/auto'

class Graph {
    chart: Chart;

    constructor(id: string, title: string, chartType: ChartType) {
        this.chart = new Chart(
            document.getElementById(id) as HTMLCanvasElement,
            {
                type: chartType,
                data: {
                    labels: [],
                    datasets: []
                },
                options: {
                    responsive: true,
                    plugins: {
                        legend: {
                            position: 'top',
                        },
                        title: {
                            display: true,
                            text: title 
                        }
                    }
                },
            });
    }

    setData(labels: Array<string>, data: any): void {
        this.chart.data.labels = labels;
        this.chart.data.datasets = data;
        this.chart.update();
    }
}

interface DataSerie {
    label: string
    data: number[]
    backgroundColor: string
}

export class Table {

    Graph: Graph
    /*colors = ["green", "blue", "red", "yellow", "purple", "pink", "teal", "grey", "brown", "orange"]*/
    colors = [
        { name: "purple", code: "#800080" },
        { name: "aliceblue", code: "#f0f8ff" },
        { name: "teal", code: "#008080" },
        { name: "darkgreen", code: "#006400" },
        { name: "firebrick", code: "#b22222" },
        { name: "olivedrab", code: "#6b8e23" },
        { name: "orange", code: "#ffa500" },
        { name: "red", code: "#ff0000" },
        { name: "rosybrown", code: "#bc8f8f" },
        { name: "yellow", code: "#ffff00" },
        { name: "fuchsia", code: "#ff00ff" },
        { name: "gainsboro", code: "#dcdcdc" },
        { name: "aqua", code: "#00ffff" },
        { name: "steelblue", code: "#4682b4" },
        { name: "tan", code: "#d2b48c" },
        { name: "aquamarine", code:  "#7fffd4" },
        { name: "azure", code:  "#f0ffff" },
        { name: "beige", code:  "#f5f5dc" },
        { name: "bisque", code:  "#ffe4c4" },
        { name: "black", code:  "#000000" },
        { name: "blanchedalmond", code:  "#ffebcd" },
        { name: "blue", code:  "#0000ff" },
        { name: "blueviolet", code:  "#8a2be2" },
        { name: "brown", code:  "#a52a2a" },
        { name: "burlywood", code:  "#deb887" },
        { name: "cadetblue", code:  "#5f9ea0" },
        { name: "chartreuse", code:  "#7fff00" },
        { name: "chocolate", code:  "#d2691e" },
        { name: "coral", code:  "#ff7f50" },
        { name: "cornflowerblue", code:  "#6495ed" },
        { name: "cornsilk", code:  "#fff8dc" },
        { name: "crimson", code:  "#dc143c" },
        { name: "cyan", code:  "#00ffff" },
        { name: "darkblue", code:  "#00008b" },
        { name: "darkcyan", code:  "#008b8b" },
        { name: "darkgoldenrod", code:  "#b8860b" },
        { name: "darkgray", code:  "#a9a9a9" },
        { name: "darkgrey", code:  "#a9a9a9" },
        { name: "darkkhaki", code:  "#bdb76b" },
        { name: "darkmagenta", code:  "#8b008b" },
        { name: "darkolivegreen", code:  "#556b2f" },
        { name: "darkorange", code:  "#ff8c00" },
        { name: "darkorchid", code:  "#9932cc" },
        { name: "darkred", code:  "#8b0000" },
        { name: "darksalmon", code:  "#e9967a" },
        { name: "darkseagreen", code:  "#8fbc8f" },
        { name: "darkslateblue", code:  "#483d8b" },
        { name: "darkslategray", code:  "#2f4f4f" },
        { name: "darkslategrey", code:  "#2f4f4f" },
        { name: "darkturquoise", code:  "#00ced1" },
        { name: "darkviolet", code:  "#9400d3" },
        { name: "deeppink", code:  "#ff1493" },
        { name: "deepskyblue", code:  "#00bfff" },
        { name: "dimgray", code:  "#696969" },
        { name: "dimgrey", code:  "#696969" },
        { name: "dodgerblue", code:  "#1e90ff" },
        { name: "floralwhite", code:  "#fffaf0" },
        { name: "forestgreen", code:  "#228b22" },
        { name: "ghostwhite", code:  "#f8f8ff" },
        { name: "goldenrod", code:  "#daa520" },
        { name: "gold", code:  "#ffd700" },
        { name: "gray", code:  "#808080" },
        { name: "green", code:  "#008000" },
        { name: "greenyellow", code:  "#adff2f" },
        { name: "grey", code:  "#808080" },
        { name: "honeydew", code:  "#f0fff0" },
        { name: "hotpink", code:  "#ff69b4" },
        { name: "indianred", code:  "#cd5c5c" },
        { name: "indigo", code:  "#4b0082" },
        { name: "ivory", code:  "#fffff0" },
        { name: "khaki", code:  "#f0e68c" },
        { name: "lavenderblush", code:  "#fff0f5" },
        { name: "lavender", code:  "#e6e6fa" },
        { name: "lawngreen", code:  "#7cfc00" },
        { name: "lemonchiffon", code:  "#fffacd" },
        { name: "lightblue", code:  "#add8e6" },
        { name: "lightcoral", code:  "#f08080" },
        { name: "lightcyan", code:  "#e0ffff" },
        { name: "lightgoldenrodyellow", code:  "#fafad2"},
        { name: "lightgray", code:  "#d3d3d3" },
        { name: "lightgreen", code:  "#90ee90" },
        { name: "lightgrey", code:  "#d3d3d3" },
        { name: "lightpink", code:  "#ffb6c1" },
        { name: "lightsalmon", code:  "#ffa07a" },
        { name: "lightseagreen", code:  "#20b2aa" },
        { name: "lightskyblue", code:  "#87cefa" },
        { name: "lightslategray", code:  "#778899" },
        { name: "lightslategrey", code:  "#778899" },
        { name: "lightsteelblue", code:  "#b0c4de" },
        { name: "lightyellow", code:  "#ffffe0" },
        { name: "lime", code:  "#00ff00" },
        { name: "limegreen", code:  "#32cd32" },
        { name: "linen", code:  "#faf0e6" },
        { name: "magenta", code:  "#ff00ff" },
        { name: "maroon", code:  "#800000" },
        { name: "mediumaquamarine", code:  "#66cdaa" },
        { name: "mediumblue", code:  "#0000cd" },
        { name: "mediumorchid", code:  "#ba55d3" },
        { name: "mediumpurple", code:  "#9370db" },
        { name: "mediumseagreen", code:  "#3cb371" },
        { name: "mediumslateblue", code:  "#7b68ee" },
        { name: "mediumspringgreen", code:  "#00fa9a" },
        { name: "mediumturquoise", code:  "#48d1cc" },
        { name: "mediumvioletred", code:  "#c71585" },
        { name: "midnightblue", code:  "#191970" },
        { name: "mintcream", code:  "#f5fffa" },
        { name: "mistyrose", code:  "#ffe4e1" },
        { name: "moccasin", code:  "#ffe4b5" },
        { name: "navajowhite", code:  "#ffdead" },
        { name: "navy", code:  "#000080" },
        { name: "oldlace", code:  "#fdf5e6" },
        { name: "olive", code:  "#808000" },
        { name: "orangered", code:  "#ff4500" },
        { name: "orchid", code:  "#da70d6" },
        { name: "palegoldenrod", code:  "#eee8aa" },
        { name: "palegreen", code:  "#98fb98" },
        { name: "paleturquoise", code:  "#afeeee" },
        { name: "palevioletred", code:  "#db7093" },
        { name: "papayawhip", code:  "#ffefd5" },
        { name: "peachpuff", code:  "#ffdab9" },
        { name: "peru", code:  "#cd853f" },
        { name: "pink", code:  "#ffc0cb" },
        { name: "plum", code:  "#dda0dd" },
        { name: "powderblue", code:  "#b0e0e6" },
        { name: "rebeccapurple", code:  "#663399" },
        { name: "royalblue", code:  "#4169e1" },
        { name: "saddlebrown", code:  "#8b4513" },
        { name: "salmon", code:  "#fa8072" },
        { name: "sandybrown", code:  "#f4a460" },
        { name: "seagreen", code:  "#2e8b57" },
        { name: "seashell", code:  "#fff5ee" },
        { name: "sienna", code:  "#a0522d" },
        { name: "silver", code:  "#c0c0c0" },
        { name: "skyblue", code:  "#87ceeb" },
        { name: "slateblue", code:  "#6a5acd" },
        { name: "slategray", code:  "#708090" },
        { name: "slategrey", code:  "#708090" },
        { name: "snow", code:  "#fffafa" },
        { name: "springgreen", code:  "#00ff7f" },
        { name: "thistle", code:  "#d8bfd8" },
        { name: "tomato", code:  "#ff6347" },
        { name: "turquoise", code:  "#40e0d0" },
        { name: "violet", code:  "#ee82ee" },
        { name: "wheat", code:  "#f5deb3" },
        { name: "white", code:  "#ffffff" },
        { name: "whitesmoke", code: "#f5f5f5" },
        { name: "antiquewhite", code: "#faebd7" },
        
        { name: "yellowgreen", code: "#9acd32" }
    ]
    colHeadAsSeries = true
    tableId: string
    hasChart: boolean
    chartId: string
    chartTitle: string

    constructor(chartId: string, chartTitle: string, tableId: string, hasChart: boolean) {
        this.hasChart = hasChart
        if (hasChart) {
            this.chartId = chartId
            this.chartTitle = chartTitle
            if (this.Graph && this.Graph.chart) {
                this.Graph.chart.destroy()
            }
            this.Graph = new Graph(chartId, chartTitle, 'bar')
        }
        this.tableId = tableId
        this.clickHandlers()
        Table.setInitialRowToggle(tableId)
        if (hasChart) {
            this.refreshChart()
        }
    }

    private clickHandlers() {
        if (this.hasChart) {
            document.querySelectorAll("#" + this.tableId + " .mox-html-table-head-row:not(.mox-html-table-head-col)").forEach(input => input.addEventListener('click', (e: Event) => this.onColHeadClick(e)));
            document.querySelectorAll("#" + this.tableId + " .mox-html-table-head-col:not(.mox-html-table-head-row)").forEach(input => input.addEventListener('click', (e: Event) => this.onRowHeadClick(e)));
        }
    }

    setChartType(chartType: ChartType) {
        this.Graph.chart.destroy()
        this.Graph = new Graph(this.chartId, this.chartTitle, chartType)
        this.refreshChart()
    }

    switchXAxis() {
        this.colHeadAsSeries = !this.colHeadAsSeries
        this.refreshChart()
    }

    private destroy() {
        if (this.hasChart) {
            this.Graph.chart.destroy();
        }
    }

    private getColor(ix: number): string {
        return this.colors[ix].code
    }

    private onColHeadClick(e) {
        let colIndex = e.srcElement.getAttribute("data-col-index")

        // set selected
        document.querySelectorAll("#" + this.tableId + " [data-col-index='" + colIndex + "']").forEach(el => el.classList.toggle("mox-html-table-column-selected"));

        //this.colHeadAsSeries = true
        this.refreshChart();
    }

    private refreshChart() {
        if (this.colHeadAsSeries) {
            this.populateChartWithColHeadAsSeries()
        }
        else {
            this.populateChartWithRowHeadAsSeries()
        }
    }

    private populateChartWithColHeadAsSeries() {

        // labels (row heads)
        let labels = [];

        let rowIds = [];
        // get all selected
        document.querySelectorAll(".mox-html-table #" + this.tableId + " .mox-html-table-head-col.mox-html-table-row-selected").forEach(el => {
            rowIds.push(el.parentElement.id);
        });

        let rowSelectedClassSelector = ".mox-html-table-row-selected"
        if (rowIds.length == 0) {
            // no selected, get all
            document.querySelectorAll(".mox-html-table #" + this.tableId + " .mox-html-table-head-col:not(.mox-html-table-head-row)").forEach(el => {
                rowIds.push(el.parentElement.id);
            });
            rowSelectedClassSelector = "";
        }

        let uniqueRowIds = [...new Set(rowIds)];

        uniqueRowIds.forEach((tr, ix) => {
            let fullTitle = '';
            document.querySelectorAll("#" + this.tableId + " #" + tr + " .mox-html-table-head-col" + rowSelectedClassSelector + ":not(.mox-html-table-head-row)").forEach(th => {
                fullTitle += th.textContent + ' ';
            });

            labels.push(fullTitle.trim());
        });


        // column heads
        let allDsets = [];
        let colIndexes = [];
        let heads = document.querySelectorAll(".mox-html-table #" + this.tableId + " .mox-html-table-head-row:not(.mox-html-table-head-col).mox-html-table-column-selected");
        heads.forEach((el) => {
            colIndexes.push((el as HTMLElement).dataset.colIndex);
        });

        if (heads.length == 0) {
            let heads = document.querySelectorAll(".mox-html-table #" + this.tableId + " .mox-html-table-head-row:not(.mox-html-table-head-col)");
            heads.forEach((el) => {
                colIndexes.push((el as HTMLElement).dataset.colIndex);
            });
        }

        let uniqueColIndexes = [...new Set(colIndexes)]

        uniqueColIndexes.forEach((ix) => {
            let dset: DataSerie = { label: "", data: [], backgroundColor: "" };
            let colTitles = [...document.querySelectorAll(".mox-html-table #" + this.tableId + " .mox-html-table-head-row:not(.mox-html-table-head-col)[data-col-index='" + ix + "']")];
            let colspanTitleAbove = document.querySelector(".mox-html-table #" + this.tableId + " .mox-html-table-head-row:not(.mox-html-table-head-col)[data-col-index='" + (ix - 1) + "'][colspan='2']")
            if (colspanTitleAbove) {
                colTitles = [colspanTitleAbove, ...colTitles];
            }
            let fullTitle = '';
            colTitles.forEach((title) => {
                fullTitle += title.textContent + ' ';
            })

            dset.label = fullTitle.trim();

            let data = [];
            let tds = [...document.querySelectorAll(".mox-html-table #" + this.tableId + " [data-col-index='" + ix + "']" + rowSelectedClassSelector + ":not(.mox-html-table-head-row:not(.mox-html-table-head-col)")];
            tds.forEach((td) => {
                let val = Table.localeParseFloat(td.textContent);
                if (!isNaN(val)) {
                    data.push(val);
                }
                else {
                    data.push(null);
                }
            })

            dset.data = data;

            dset.backgroundColor = this.getColor(ix);
            allDsets.push(dset);
        });

        this.Graph.setData(labels, allDsets);
    }

    private onRowHeadClick(e) {
        if (e.srcElement.classList.contains("fas")) {
            // toggler was clicked
            return;
        }

        // set selected
        document.querySelectorAll(".mox-html-table #" + this.tableId + " #" + e.srcElement.parentElement.id + " > *").forEach(el => el.classList.toggle("mox-html-table-row-selected"));

        //this.colHeadAsSeries = false;
        this.refreshChart();
        
    }

    private populateChartWithRowHeadAsSeries() {
        // labels (column heads)
        let labels = [];

        let colIndexes = [];
        let heads = document.querySelectorAll(".mox-html-table #" + this.tableId + " .mox-html-table-head-row:not(.mox-html-table-head-col).mox-html-table-column-selected");
        heads.forEach((el) => {
            colIndexes.push((el as HTMLElement).dataset.colIndex);
        });

        let colsSelectedClassSelector = ".mox-html-table-column-selected"
        if (heads.length == 0) {
            heads = document.querySelectorAll(".mox-html-table #" + this.tableId + " .mox-html-table-head-row:not(.mox-html-table-head-col)");
            heads.forEach((el) => {
                colIndexes.push((el as HTMLElement).dataset.colIndex);
            });
            colsSelectedClassSelector = "";
        }

        let uniqueColIndexes = [...new Set(colIndexes)]

        uniqueColIndexes.forEach((ix) => {
            let colTitles = [...document.querySelectorAll(".mox-html-table #" + this.tableId + " .mox-html-table-head-row:not(.mox-html-table-head-col)[data-col-index='" + ix + "']")]
            let colspanTitleAbove = document.querySelector(".mox-html-table #" + this.tableId + " .mox-html-table-head-row:not(.mox-html-table-head-col)[data-col-index='" + (ix - 1) + "'][colspan='2']")
            if (colspanTitleAbove) {
                colTitles = [colspanTitleAbove, ...colTitles];
            }
            let fullTitle = '';
            colTitles.forEach((title) => {
                fullTitle += title.textContent + ' ';
            })
            labels.push(fullTitle.trim());
        });

        // row heads
        let rowIds = [];
        // get all selected

        let rowSelectedClassSelector = ".mox-html-table-row-selected"
        if (document.querySelectorAll(".mox-html-table #" + this.tableId + " .mox-html-table-head-col" + rowSelectedClassSelector + ":not(.mox-html-table-head-row)").length == 0) {
            rowSelectedClassSelector = "";
        }

        document.querySelectorAll(".mox-html-table #" + this.tableId + " .mox-html-table-head-col" + rowSelectedClassSelector + ":not(.mox-html-table-head-row)").forEach(el => {
            rowIds.push(el.parentElement.id);
        });

        let uniqueRowIds = [...new Set(rowIds)];

        let allDsets = [];

        uniqueRowIds.forEach((tr, ix) => {
            let dset: DataSerie = { label: "", data: [], backgroundColor: "" };

            let fullTitle = '';
            document.querySelectorAll("#" + this.tableId + " #" + tr + " .mox-html-table-head-col" + rowSelectedClassSelector + ":not(.mox-html-table-head-row)").forEach(th => {
                fullTitle += th.textContent + ' ';
            });

            dset.label = fullTitle.trim();

            if (document.querySelectorAll("#" + this.tableId + " #" + tr + " " + rowSelectedClassSelector + colsSelectedClassSelector + ":not(.mox-html-table-head-col):not(i)").length == 0) {
                rowSelectedClassSelector = "> *";
            }

            let data = [];
            document.querySelectorAll("#" + this.tableId + " #" + tr + " " + rowSelectedClassSelector + colsSelectedClassSelector + ":not(.mox-html-table-head-col):not(i)").forEach(td => {
                let val = Table.localeParseFloat(td.textContent);
                if (!isNaN(val)) {
                    data.push(val);
                }
                else {
                    data.push(null);
                }
            });

            dset.data = data;
            dset.backgroundColor = this.getColor(ix);
            allDsets.push(dset);

        });

        this.Graph.setData(labels, allDsets);
    }

    private static setInitialRowToggle(tableId: string) {
        document.querySelectorAll(".mox-html-table #" + tableId + " i.fas").forEach(toggler => {
            let onclickFunction: string = toggler.getAttribute("onclick")
            let show = true;
            if (onclickFunction.indexOf("true") >= 0) {
                show = false
            }
            let rowsToToggle = Table.getTogglerArgs(onclickFunction, tableId);
            this.toggleRowsExplicitArray(toggler as HTMLElement, tableId, show, rowsToToggle);
        });
    }

    private static toggleRowsExplicitArray(el: HTMLElement, tableId: string, show: boolean, rowsToToggle: number[]) {
        for (let row = 0; row < rowsToToggle.length; row++) {
            if (document.querySelector("#" + tableId + " #Row_" + rowsToToggle[row].toString())) {
                if (show) {
                    document.querySelector("#" + tableId + " #Row_" + rowsToToggle[row].toString()).removeAttribute("style");
                }
                else {
                    document.querySelector("#" + tableId + " #Row_" + rowsToToggle[row].toString()).setAttribute("style", "display:none;");
                }
            }
        }
        if (el) {
            this.updateToggler(el, show);
        }
    }

    static toggleRows(el: HTMLElement, tableId: string, show: boolean, ...rowsToToggle: number[]) {
        this.toggleRowsExplicitArray(el, tableId, show, rowsToToggle);
    }

    static toggleAllRows(tableId: string, show: boolean) {
        let allRows = document.querySelectorAll(".mox-html-table #" + tableId + " tr");
        for (let i = 0; i < allRows.length; i++) {
            let toggler = allRows[i].querySelector("i");
            if (toggler) {
                let args = Table.getTogglerArgs(toggler.getAttribute("onclick"), tableId); 
                this.toggleRowsExplicitArray(toggler, tableId, show, args);
            }
        }
    }

    private static getTogglerArgs(args: string, tableId: string) : number[] {
        let xx = args.replace(tableId, "").replace("true", "").replace("false", "").replace("HtmlTable.Table.toggleRows(this,,,", "").replace(")", "").split(',').map(v => parseInt(v));
        return xx;
    }

    private static updateToggler(toggler: HTMLElement, show: boolean) {
        if (show) {
            toggler.classList.value = "fas fa-minus";
            toggler.setAttribute("onclick", toggler.getAttribute("onclick").replace("true", "false"));
        }
        else {
            toggler.classList.value = "fas fa-plus";
            toggler.setAttribute("onclick", toggler.getAttribute("onclick").replace("false", "true"));
        }
    }

    private static localeParseFloat(s: string) {
        // Get the thousands and decimal separator characters used in the locale.
        let [, thousandsSeparator, , , , decimalSeparator] = 1111.1.toLocaleString();
        // Remove thousand separators, and put a point where the decimal separator occurs
        s = Array.from(s, c => c === thousandsSeparator ? ""
            : c === decimalSeparator ? "." : c).join("");
        // Now it can be parsed
        return parseFloat(s);
    }
}