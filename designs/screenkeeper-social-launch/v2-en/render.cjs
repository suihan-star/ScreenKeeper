const fs=require("node:fs"),path=require("node:path"),{spawn}=require("node:child_process"),{once}=require("node:events"),crypto=require("node:crypto");
let chromium;
try{({chromium}=require(process.env.SCREENKEEPER_PLAYWRIGHT||"playwright"));}
catch{console.error("找不到 Playwright。请设置 SCREENKEEPER_PLAYWRIGHT 指向本地模块，或提供 playwright 模块。\nPlaywright is unavailable. Set SCREENKEEPER_PLAYWRIGHT to a local module, or make playwright resolvable.");process.exit(1);}
const root=__dirname, mode=process.argv[2]||"qa",base=process.env.SCREENKEEPER_PREVIEW_URL||"http://127.0.0.1:4311/screenkeeper-social-launch/v2-en/";
const chrome=process.env.SCREENKEEPER_CHROME||"C:/Program Files/Google/Chrome/Application/chrome.exe";
const clips=["01-screen-rest","02-privacy","03-health","04-color"];
const times=[0.3,2.85,3.15,5.85,6.15,9.2,11.85,12.15,14.5,15.85,16.15,17.95];
function hash(b){return crypto.createHash("sha256").update(b).digest("hex")}
function ffmpegPath(){if(process.env.SCREENKEEPER_FFMPEG)return process.env.SCREENKEEPER_FFMPEG;const p=path.resolve(root,"../../../artifacts/social-video-tools/imageio_ffmpeg/binaries");return path.join(p,fs.readdirSync(p).find(x=>x.endsWith(".exe")&&x.startsWith("ffmpeg")))}
function runProcess(exe,args){return new Promise((resolve,reject)=>{const p=spawn(exe,args,{windowsHide:true});let err="";p.stderr.on("data",b=>err+=b);p.on("error",reject);p.on("close",c=>c===0?resolve(err):reject(new Error(err.slice(-2400))));})}
async function open(browser,clip){
 const page=await browser.newPage({viewport:{width:900,height:1200},deviceScaleFactor:1});
 const errors=[];page.on("pageerror",e=>errors.push(e.message));
 await page.goto(base+"motion/"+clip+".html?capture",{waitUntil:"networkidle"});
 await page.waitForFunction(()=>window.__animStage&&document.querySelector("[data-om-sync-seek]"));
 await page.evaluate(async()=>{await document.fonts.ready;await Promise.all([...document.images].map(i=>i.decode().catch(()=>{})));window.__animStage.setPlaying(false);window.__animStage.setTime(0);});
 const info=await page.evaluate(()=>({roots:document.querySelectorAll("[data-om-exportable-video-with-duration-secs]").length,duration:window.__animStage.duration,broken:[...document.images].filter(i=>!i.naturalWidth).map(i=>i.src)}));
 if(errors.length||info.roots!==1||info.duration!==18||info.broken.length)throw new Error(JSON.stringify({clip,errors,info}));
 return {page,errors};
}
async function seek(page,t){await page.evaluate(t=>window.__animStage.setTime(t),t)}
async function qa(browser,clip){
 const {page,errors}=await open(browser,clip);const entries=[];
 for(let n=0;n<times.length;n++){
  const t=times[n];await seek(page,t);const file=clip+"-"+String(n).padStart(2,"0")+".png";
  await page.screenshot({path:path.join(root,"qa",file)});
  entries.push(await page.evaluate(({file,t})=>({file,t,state:document.querySelector(".piece").dataset.state,blackout:document.querySelector("[data-blackout]")?.style.opacity,curtain:document.querySelector("[data-curtain]")?.style.opacity,health:document.querySelector("[data-health-state]")?.dataset.healthState,warm:document.querySelector("[data-warm]")?.style.opacity,caption:document.querySelector("[data-om-caption]")?.textContent}),{file,t}));
 }
 // Loop endpoints are rendered with all overlays removed and matching caption.
 await seek(page,0);const start=await page.screenshot();
 await seek(page,17.999);const end=await page.screenshot();
 const loopMatches=hash(start)===hash(end);
 if(!loopMatches)throw new Error(clip+": loop seam differs");
 if(clip.includes("privacy")&&(entries[5].curtain!=="1"||entries[8].curtain!=="0"))throw new Error("Privacy curtain semantic states incorrect");
 if(clip.includes("screen-rest")&&(entries[5].blackout!=="1"||entries[8].blackout!=="0"))throw new Error("Rest recovery states incorrect");
 if(clip.includes("health")&&(entries[2].health!=="reminder"||entries[5].health!=="resting"))throw new Error("Health transitions incorrect");
 if(clip.includes("color")&&!(Number(entries[5].warm)>0.5&&Number(entries[8].warm)===0))throw new Error("Color comparison states incorrect");
 const filmstrip='<!doctype html><html lang="en"><meta charset="utf-8"><style>body{margin:0;padding:18px;background:#17372e;font:16px "Segoe UI";color:#e8efe5}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:14px}figure{margin:0}img{width:100%;display:block}figcaption{height:28px;padding-top:4px}</style><body><div class="grid">'+entries.map(e=>'<figure><img src="'+e.file+'"><figcaption>'+e.t.toFixed(2)+'s · '+e.state+'</figcaption></figure>').join("")+'</div></body></html>';
 fs.writeFileSync(path.join(root,"qa",clip+"-filmstrip.html"),filmstrip);
 await page.setViewportSize({width:960,height:1840});await page.goto(base+"qa/"+clip+"-filmstrip.html",{waitUntil:"networkidle"});
 await page.screenshot({path:path.join(root,"qa",clip+"-filmstrip.png"),fullPage:true});await page.close();
 console.log("QA "+clip+": 12 boundary frames, exact loop match, states verified.");
 return {clip,errors,loopMatches,entries};
}
async function render(browser,clip){
 const {page,errors}=await open(browser,clip);const fps=24,frames=18*fps,file=path.join(root,"exports",clip+".mp4"),ffmpeg=ffmpegPath();
 const args=["-hide_banner","-loglevel","error","-y","-f","image2pipe","-vcodec","png","-framerate",String(fps),"-i","pipe:0","-an","-c:v","libx264","-preset","fast","-crf","20","-pix_fmt","yuv420p","-movflags","+faststart",file];
 const proc=spawn(ffmpeg,args,{windowsHide:true,stdio:["pipe","ignore","pipe"]});let err="";proc.stderr.on("data",b=>err+=b);const done=new Promise((resolve,reject)=>{proc.on("error",reject);proc.on("close",c=>c===0?resolve():reject(new Error(err)));});
 for(let i=0;i<frames;i++){await seek(page,i/fps);const png=await page.screenshot({type:"png"});if(!proc.stdin.write(png))await once(proc.stdin,"drain");if(i%144===0)console.log(clip+" "+Math.round(i/frames*100)+"%");}
 proc.stdin.end();await done;await page.close();if(errors.length)throw new Error(errors.join("\n"));
 const gif=path.join(root,"exports",clip+".gif");
 const palette=clip.includes("color")?"palettegen=max_colors=192:stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=3":"palettegen=stats_mode=diff[p];[b][p]paletteuse=dither=sierra2_4a";
 await runProcess(ffmpeg,["-hide_banner","-loglevel","error","-y","-i",file,"-filter_complex","fps=12,scale=540:720:flags=lanczos,split[a][b];[a]"+palette,"-loop","0",gif]);
 const decodeLog=await runProcess(ffmpeg,["-hide_banner","-i",file,"-f","null","-"]);
 console.log("Exported "+clip+": MP4 + GIF, MP4 fully decoded.");
 return {clip,duration:18,fps,frames,mp4Bytes:fs.statSync(file).size,gifBytes:fs.statSync(gif).size,mp4Sha256:hash(fs.readFileSync(file)),gifSha256:hash(fs.readFileSync(gif)),decodePassed:true,decodeLog:decodeLog.slice(-1000)};
}
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:chrome});
 try{
  const results=[];
  // Two browser pages at a time avoid saturating the user's machine.
  for(let i=0;i<clips.length;i+=2){results.push(...await Promise.all(clips.slice(i,i+2).map(c=>mode==="qa"?qa(browser,c):render(browser,c))));}
  fs.writeFileSync(path.join(root,"qa",mode==="qa"?"verification.json":"export-verification.json"),JSON.stringify(results,null,2));
 }finally{await browser.close();}
})().catch(e=>{console.error(e.stack);process.exitCode=1});
