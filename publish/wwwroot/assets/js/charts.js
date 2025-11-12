// helpers
function avg(nums){ return nums.reduce((a,b)=>a+b,0)/Math.max(nums.length,1); }

const baseOpts = {
  responsive: true,
  plugins: {
    legend: { display: false },
    tooltip: {
      enabled: true,
      callbacks: {
        label: (ctx) => {
          const v = ctx.raw;
          return typeof v === "number" ? `${ctx.dataset.label || ""} ${v.toFixed(1)}` : v;
        }
      }
    }
  },
  layout: { padding: 8 }
};

// 1) Bar: Avg grade by course
(function(){
  const el = document.getElementById("barCourse"); if(!el) return;
  const byCourse = {};
  enrollments.forEach(e=>{
    const c = courses.find(x=>x.id===e.courseId)?.title || "Unknown";
    (byCourse[c] = byCourse[c] || []).push(e.numeric_grade);
  });
  const labels = Object.keys(byCourse);
  const data = labels.map(k=>avg(byCourse[k]));
  new Chart(el,{
    type:"bar",
    data:{ labels, datasets:[{ label:"Avg Grade", data }] },
    options:{
      ...baseOpts,
      scales:{
        y: { beginAtZero:true, suggestedMax:100, ticks:{ stepSize:10 }, grid:{ color:"#e5e7eb" } },
        x: { ticks:{ maxRotation: 20, minRotation: 0 }, grid:{ display:false } }
      }
    }
  });
})();

// 2) Pie: Students by Major
(function(){
  const el = document.getElementById("pieMajor"); if(!el) return;
  const count = {};
  students.forEach(s=>{ count[s.major]=(count[s.major]||0)+1; });
  new Chart(el,{
    type:"pie",
    data:{ labels:Object.keys(count), datasets:[{ data:Object.values(count) }] },
    options:{
      responsive:true,
      plugins:{ legend:{ display:true, position:"bottom" } }
    }
  });
})();

// 3) Line: Avg grade by term
(function(){
  const el = document.getElementById("lineTerm"); if(!el) return;
  const order = ["Spring 2025","Fall 2025"];
  const byTerm = {};
  enrollments.forEach(e=>{ (byTerm[e.term]=byTerm[e.term]||[]).push(e.numeric_grade); });
  const labels = order.filter(t=>byTerm[t]);
  const data = labels.map(t=>avg(byTerm[t]));
  new Chart(el,{
    type:"line",
    data:{ labels, datasets:[{ label:"Avg Grade", data, tension:0.3, pointRadius:4 }] },
    options:{
      ...baseOpts,
      scales:{
        y:{ suggestedMin:70, suggestedMax:100, grid:{ color:"#e5e7eb" } },
        x:{ grid:{ display:false } }
      }
    }
  });
})();
