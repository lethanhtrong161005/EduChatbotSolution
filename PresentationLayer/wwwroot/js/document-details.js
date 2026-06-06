// document.addEventListener(
//     "DOMContentLoaded",
//     async function () {
//         const anchorDiv = document.getElementById("chunk-showcase-table");
//         const docId = anchorDiv.dataset.docId;

//         const respsonse = await fetch(`/documents/chunk-table/${docId}`);

//         const html = await respsonse.text();

//         const parser = new DOMParser();
//         const doc = parser.parseFromString(html, "text/html");
//         console.log(doc);

//         anchorDiv.innerHTML = doc.documentElement.innerHTML;
//     });