/* itsCRA1G "follow me" strip, shown on every page of the hub.
   To change a link, edit it below. Leave a link as "" to hide that button. */
(function(){
"use strict";
var LINKS={
  twitch:   "https://twitch.tv/itscra1g",
  kick:     "https://kick.com/itscra1g",
  youtube:  "",
  tiktok:   "",
  discord:  "",
  x:        "",
  instagram:"",
  kofi:     "https://ko-fi.com/itscra1g"
};
var NAME="itsCRA1G";
/* ---------------------------------------------------------------- */
var de=document.documentElement;
if(de.classList.contains("ov")||/(^#|&)ov=1(&|$)/.test(location.hash))return; // never in an OBS overlay
var P={
 twitch:["Twitch","#9146FF",'<path d="M4 2 2.5 6v14h5v2.5h2.8L13 20h4l4.5-4.5V2H4Zm15.5 12.6L17 17h-4.5L10 19.5V17H6V4h13.5v10.6ZM15 7.5h2v6h-2v-6Zm-5 0h2v6h-2v-6Z"/>'],
 kick:["Kick","#53FC18",'<path d="M3 3h6v5h2V6h2V3h7v6h-2v2h-2v2h2v2h2v6h-7v-3h-2v-2H9v5H3V3Z"/>'],
 youtube:["YouTube","#FF0033",'<path d="M21.6 7.2a2.5 2.5 0 0 0-1.8-1.8C18.2 5 12 5 12 5s-6.2 0-7.8.4A2.5 2.5 0 0 0 2.4 7.2C2 8.8 2 12 2 12s0 3.2.4 4.8a2.5 2.5 0 0 0 1.8 1.8C5.8 19 12 19 12 19s6.2 0 7.8-.4a2.5 2.5 0 0 0 1.8-1.8c.4-1.6.4-4.8.4-4.8s0-3.2-.4-4.8ZM10 15V9l5.2 3L10 15Z"/>'],
 tiktok:["TikTok","#FF2D6F",'<path d="M16.5 2h-3.3v13.2a2.9 2.9 0 1 1-2.1-2.8V9a6.3 6.3 0 1 0 5.4 6.2V8.6a7.8 7.8 0 0 0 4.5 1.4V6.7A4.6 4.6 0 0 1 16.5 2Z"/>'],
 discord:["Discord","#5865F2",'<path d="M19.3 5.3A16.5 16.5 0 0 0 15.2 4l-.5 1a15 15 0 0 0-5.4 0l-.5-1a16.5 16.5 0 0 0-4.1 1.3C2.1 9.2 1.4 13 1.7 16.8a16.6 16.6 0 0 0 5 2.5l1.1-1.7c-.6-.2-1.2-.5-1.7-.9l.4-.3a11.8 11.8 0 0 0 11 0l.4.3c-.5.4-1.1.7-1.7.9l1.1 1.7a16.6 16.6 0 0 0 5-2.5c.4-4.4-.7-8.2-3-11.5ZM8.7 14.5c-1 0-1.8-.9-1.8-2s.8-2 1.8-2 1.8.9 1.8 2-.8 2-1.8 2Zm6.6 0c-1 0-1.8-.9-1.8-2s.8-2 1.8-2 1.8.9 1.8 2-.8 2-1.8 2Z"/>'],
 x:["X","#E7E9EA",'<path d="M17.8 3h3.1l-6.8 7.8L22 21h-6.2l-4.9-6.4L5.3 21H2.2l7.3-8.3L2 3h6.4l4.4 5.8L17.8 3Zm-1.1 16.2h1.7L7.4 4.7H5.6l11.1 14.5Z"/>'],
 instagram:["Instagram","#FF4FA0",'<path d="M12 7.3a4.7 4.7 0 1 0 0 9.4 4.7 4.7 0 0 0 0-9.4Zm0 7.7a3 3 0 1 1 0-6 3 3 0 0 1 0 6Zm4.9-8.9a1.1 1.1 0 1 0 0 2.2 1.1 1.1 0 0 0 0-2.2ZM16.5 2h-9A5.5 5.5 0 0 0 2 7.5v9A5.5 5.5 0 0 0 7.5 22h9a5.5 5.5 0 0 0 5.5-5.5v-9A5.5 5.5 0 0 0 16.5 2Zm3.7 14.5a3.7 3.7 0 0 1-3.7 3.7h-9a3.7 3.7 0 0 1-3.7-3.7v-9a3.7 3.7 0 0 1 3.7-3.7h9a3.7 3.7 0 0 1 3.7 3.7v9Z"/>'],
 kofi:["Ko-fi","#FF5E5B",'<path d="M3 5h14.5a4 4 0 0 1 0 8H17v1a5 5 0 0 1-5 5H8a5 5 0 0 1-5-5V5Zm14 2v4h.5a2 2 0 0 0 0-4H17Zm-7.2 2.2c-.9-.9-2.5-.3-2.5 1.1 0 1.3 1.5 2.4 2.7 3.3 1.2-.9 2.7-2 2.7-3.3 0-1.4-1.6-2-2.5-1.1l-.2.2-.2-.2Z"/>']
};
var ORDER=["twitch","kick","youtube","tiktok","discord","x","instagram","kofi"];
var css=
'.flw{--fa:var(--acc,#4DA3FF);margin:40px 0 36px;padding:22px 22px;border:1px solid var(--edge,#1f2329);border-radius:18px;background:linear-gradient(120deg,rgba(77,163,255,.10),rgba(77,163,255,0) 55%),var(--tile,#0b0d10);display:flex;align-items:center;justify-content:space-between;gap:16px 24px;flex-wrap:wrap}'+
'.flw .ft{min-width:0;flex:1 1 260px}'+
'.flw .fe{font:700 11px Inter,system-ui,sans-serif;letter-spacing:1.6px;text-transform:uppercase;color:var(--fa);display:flex;align-items:center;gap:8px}'+
'.flw .fe i{width:8px;height:8px;border-radius:50%;background:#FF3B3B;box-shadow:0 0 0 0 rgba(255,59,59,.6);animation:flwp 2s infinite}'+
'@keyframes flwp{70%{box-shadow:0 0 0 8px rgba(255,59,59,0)}100%{box-shadow:0 0 0 0 rgba(255,59,59,0)}}'+
'.flw h2{margin:6px 0 4px;font:700 22px/1.2 "Space Grotesk",Inter,system-ui,sans-serif;color:var(--fg,#fff);text-wrap:balance}'+
'.flw p{margin:0;font:400 14.5px/1.5 Inter,system-ui,sans-serif;color:var(--body,#b8c0cc)}'+
'.flw .fl{display:flex;flex-wrap:wrap;gap:8px}'+
'.flw .fl a{--c:#fff;display:inline-flex;align-items:center;gap:8px;padding:9px 14px 9px 11px;border-radius:12px;background:rgba(255,255,255,.04);border:1px solid var(--edge2,#2a2f37);color:var(--fg,#fff);font:600 14px Inter,system-ui,sans-serif;text-decoration:none;transition:border-color .15s,background .15s,transform .15s}'+
'.flw .fl a svg{width:18px;height:18px;fill:var(--c);flex:0 0 auto}'+
'.flw .fl a:hover{border-color:var(--c);background:rgba(255,255,255,.08);transform:translateY(-1px)}'+
'.flw .fl a:focus-visible{outline:2px solid var(--fa);outline-offset:2px}'+
'.flw.big{margin:18px 0 6px;padding:26px}'+
'.flw.big h2{font-size:26px}'+
'@media (max-width:560px){.flw{padding:18px}.flw .fl a{padding:8px 12px 8px 10px;font-size:13.5px}}'+
'@media (prefers-reduced-motion:reduce){.flw .fe i{animation:none}.flw .fl a{transition:none}}'+
'html.ov .flw{display:none!important}';
function btns(){
  return ORDER.filter(function(k){return LINKS[k]}).map(function(k){var p=P[k];
    return '<a href="'+LINKS[k]+'" target="_blank" rel="noopener" style="--c:'+p[1]+'" aria-label="'+NAME+' on '+p[0]+'"><svg viewBox="0 0 24 24" aria-hidden="true">'+p[2]+'</svg>'+p[0]+'</a>'}).join("");
}
function block(big){
  var el=document.createElement("aside");el.className="flw"+(big?" big":"");el.setAttribute("aria-label","Follow "+NAME);
  el.innerHTML='<div class="ft"><div class="fe"><i></i>Used live on stream</div>'+
    '<h2>'+(big?"Come and hang out with "+NAME:"Enjoying this? Come say hi.")+'</h2>'+
    '<p>'+(big?"Everything here runs on my own streams. Come see it in action, ask questions in chat, and catch new tools as they land.":"I use these on my own streams as "+NAME+". A follow helps more than you'd think.")+'</p></div>'+
    '<div class="fl">'+btns()+'</div>';
  return el;
}
function run(){
  if(document.querySelector(".flw"))return;
  var st=document.createElement("style");st.textContent=css;document.head.appendChild(st);
  var slot=document.getElementById("follow-hero");
  if(slot){slot.appendChild(block(true))}
  var f=document.querySelector("footer");
  if(f&&!slot){
    // put it just above the footer, inside the same column
    var inner=f.querySelector(".wrap");
    if(inner){var w=document.createElement("div");w.className="wrap";w.appendChild(block(false));f.parentNode.insertBefore(w,f)}
    else f.parentNode.insertBefore(block(false),f);
  }else if(!f&&!slot){
    var d=document.createElement("div");d.className="wrap";d.style.paddingBottom="40px";d.appendChild(block(false));document.body.appendChild(d);
  }
}
if(document.readyState==="loading")document.addEventListener("DOMContentLoaded",run);else run();
})();
