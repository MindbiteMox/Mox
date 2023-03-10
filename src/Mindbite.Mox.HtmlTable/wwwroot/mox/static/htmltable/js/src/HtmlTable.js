import Chart from 'Chart.js/auto';
class Graph {
    constructor(id, title) {
        this.chart = new Chart(document.getElementById(id), {
            type: 'bar',
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
    setData(labels, data) {
        this.chart.data.labels = labels;
        this.chart.data.datasets = data;
        this.chart.update();
    }
}
export class Table {
    constructor(chartId, title) {
        this.colors = ["green", "blue", "red", "yellow", "purple", "pink", "teal", "grey", "brown", "orange"];
        this.colHeadAsSeries = true;
        this.Graph = new Graph(chartId, title);
        this.clickHandlers();
        Table.setInitialRowToggle();
    }
    clickHandlers() {
        document.querySelectorAll(".mox-html-table-head-row:not(.mox-html-table-head-col)").forEach(input => input.addEventListener('click', (e) => this.onColHeadClick(e)));
        document.querySelectorAll(".mox-html-table-head-col:not(.mox-html-table-head-row)").forEach(input => input.addEventListener('click', (e) => this.onRowHeadClick(e)));
    }
    getColor(ix) {
        return this.colors[ix];
    }
    onColHeadClick(e) {
        let colIndex = e.srcElement.getAttribute("data-col-index");
        // set selected
        document.querySelectorAll("[data-col-index='" + colIndex + "']").forEach(el => el.classList.toggle("mox-html-table-column-selected"));
        this.colHeadAsSeries = true;
        this.refreshChart();
    }
    refreshChart() {
        if (this.colHeadAsSeries) {
            this.populateChartWithColHeadAsSeries();
        }
        else {
            this.populateChartWithRowHeadAsSeries();
        }
    }
    populateChartWithColHeadAsSeries() {
        // labels (row heads)
        let labels = [];
        let rowIds = [];
        // get all selected
        document.querySelectorAll(".mox-html-table .mox-html-table-head-col.mox-html-table-row-selected").forEach(el => {
            rowIds.push(el.parentElement.id);
        });
        let rowSelectedClassSelector = ".mox-html-table-row-selected";
        if (rowIds.length == 0) {
            // no selected, get all
            document.querySelectorAll(".mox-html-table .mox-html-table-head-col:not(.mox-html-table-head-row)").forEach(el => {
                rowIds.push(el.parentElement.id);
            });
            rowSelectedClassSelector = "";
        }
        let uniqueRowIds = [...new Set(rowIds)];
        uniqueRowIds.sort().forEach((tr, ix) => {
            let fullTitle = '';
            document.querySelectorAll("#" + tr + " .mox-html-table-head-col" + rowSelectedClassSelector + ":not(.mox-html-table-head-row)").forEach(th => {
                fullTitle += th.textContent + ' ';
            });
            labels.push(fullTitle.trim());
        });
        // column heads
        let allDsets = [];
        let colIndexes = [];
        let heads = document.querySelectorAll(".mox-html-table .mox-html-table-head-row:not(.mox-html-table-head-col).mox-html-table-column-selected");
        heads.forEach((el) => {
            colIndexes.push(el.dataset.colIndex);
        });
        if (heads.length == 0) {
            let heads = document.querySelectorAll(".mox-html-table .mox-html-table-head-row:not(.mox-html-table-head-col)");
            heads.forEach((el) => {
                colIndexes.push(el.dataset.colIndex);
            });
        }
        let uniqueColIndexes = [...new Set(colIndexes)];
        uniqueColIndexes.sort().forEach((ix) => {
            let dset = { label: "", data: [], backgroundColor: "" };
            let colTitles = [...document.querySelectorAll(".mox-html-table .mox-html-table-head-row:not(.mox-html-table-head-col)[data-col-index='" + ix + "']")];
            let fullTitle = '';
            colTitles.forEach((title) => {
                fullTitle += title.textContent + ' ';
            });
            dset.label = fullTitle.trim();
            let data = [];
            let tds = [...document.querySelectorAll(".mox-html-table [data-col-index='" + ix + "']:not(.mox-html-table-head-row:not(.mox-html-table-head-col)")];
            tds.forEach((td) => {
                data.push(td.textContent);
            });
            dset.data = data;
            dset.backgroundColor = this.getColor(ix);
            allDsets.push(dset);
        });
        this.Graph.setData(labels, allDsets);
    }
    onRowHeadClick(e) {
        if (e.srcElement.classList.contains("fas")) {
            // toggler was clicked
            return;
        }
        // set selected
        document.querySelectorAll("#" + e.srcElement.parentElement.id + " > *").forEach(el => el.classList.toggle("mox-html-table-row-selected"));
        this.colHeadAsSeries = false;
        this.refreshChart();
    }
    populateChartWithRowHeadAsSeries() {
        // labels (column heads)
        let labels = [];
        let colIndexes = [];
        let heads = document.querySelectorAll(".mox-html-table .mox-html-table-head-row:not(.mox-html-table-head-col).mox-html-table-column-selected");
        heads.forEach((el) => {
            colIndexes.push(el.dataset.colIndex);
        });
        let colsSelectedClassSelector = ".mox-html-table-column-selected";
        if (heads.length == 0) {
            heads = document.querySelectorAll(".mox-html-table .mox-html-table-head-row:not(.mox-html-table-head-col)");
            heads.forEach((el) => {
                colIndexes.push(el.dataset.colIndex);
            });
            colsSelectedClassSelector = "";
        }
        let uniqueColIndexes = [...new Set(colIndexes)];
        uniqueColIndexes.sort().forEach((ix) => {
            let colTitles = [...document.querySelectorAll(".mox-html-table .mox-html-table-head-row:not(.mox-html-table-head-col)[data-col-index='" + ix + "']")];
            let fullTitle = '';
            colTitles.forEach((title) => {
                fullTitle += title.textContent + ' ';
            });
            labels.push(fullTitle.trim());
        });
        // row heads
        let rowIds = [];
        // get all selected
        let rowSelectedClassSelector = ".mox-html-table-row-selected";
        if (document.querySelectorAll(".mox-html-table .mox-html-table-head-col" + rowSelectedClassSelector + ":not(.mox-html-table-head-row)").length == 0) {
            rowSelectedClassSelector = "";
        }
        document.querySelectorAll(".mox-html-table .mox-html-table-head-col" + rowSelectedClassSelector + ":not(.mox-html-table-head-row)").forEach(el => {
            rowIds.push(el.parentElement.id);
        });
        let uniqueRowIds = [...new Set(rowIds)];
        let allDsets = [];
        uniqueRowIds.sort().forEach((tr, ix) => {
            let dset = { label: "", data: [], backgroundColor: "" };
            let fullTitle = '';
            document.querySelectorAll("#" + tr + " .mox-html-table-head-col" + rowSelectedClassSelector + ":not(.mox-html-table-head-row)").forEach(th => {
                fullTitle += th.textContent + ' ';
            });
            dset.label = fullTitle.trim();
            if (document.querySelectorAll("#" + tr + " " + rowSelectedClassSelector + colsSelectedClassSelector + ":not(.mox-html-table-head-col):not(i)").length == 0) {
                rowSelectedClassSelector = "> *";
            }
            let data = [];
            document.querySelectorAll("#" + tr + " " + rowSelectedClassSelector + colsSelectedClassSelector + ":not(.mox-html-table-head-col):not(i)").forEach(td => {
                data.push(td.textContent);
            });
            dset.data = data;
            dset.backgroundColor = this.getColor(ix);
            allDsets.push(dset);
        });
        this.Graph.setData(labels, allDsets);
    }
    static setInitialRowToggle() {
        document.querySelectorAll(".mox-html-table i.fas").forEach(toggler => {
            let onclickFunction = toggler.getAttribute("onclick");
            let show = true;
            if (onclickFunction.indexOf("true") >= 0) {
                show = false;
            }
            let rowsToToggle = onclickFunction.replace(" true", "").replace(" false", "").replace("HtmlTable.Table.toggleRows(this,, ", "").replace(")", "").split(',').map(v => parseInt(v));
            this.toggleRowsExplicitArray(toggler, show, rowsToToggle);
        });
    }
    static toggleRowsExplicitArray(el, show, rowsToToggle) {
        for (let row = 0; row < rowsToToggle.length; row++) {
            if (document.getElementById("Row_" + rowsToToggle[row].toString())) {
                if (show) {
                    document.getElementById("Row_" + rowsToToggle[row].toString()).removeAttribute("style");
                }
                else {
                    document.getElementById("Row_" + rowsToToggle[row].toString()).setAttribute("style", "display:none;");
                }
            }
        }
        if (el) {
            this.updateToggler(el, show);
        }
    }
    static toggleRows(el, show, ...rowsToToggle) {
        this.toggleRowsExplicitArray(el, show, rowsToToggle);
    }
    static toggleAllRows(show) {
        let allRows = document.querySelectorAll(".mox-html-table tr");
        for (let i = 0; i < allRows.length; i++) {
            let toggler = allRows[i].querySelector("i");
            if (toggler) {
                let args = toggler.getAttribute("onclick").replace(" true", "").replace(" false", "").replace("HtmlTable.Table.toggleRows(this,, ", "").replace(")", "").split(',').map(v => parseInt(v));
                this.toggleRowsExplicitArray(toggler, show, args);
            }
        }
    }
    static updateToggler(toggler, show) {
        if (show) {
            toggler.classList = "fas fa-minus";
            toggler.setAttribute("onclick", toggler.getAttribute("onclick").replace("true", "false"));
        }
        else {
            toggler.classList = "fas fa-plus";
            toggler.setAttribute("onclick", toggler.getAttribute("onclick").replace("false", "true"));
        }
    }
}
//# sourceMappingURL=HtmlTable.js.map