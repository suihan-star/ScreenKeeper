const fs=require("node:fs"),path=require("node:path");
const babelPath=process.env.SCREENKEEPER_BABEL||"<user-home>/.codex/skills/baoyu-design/agents/vendor/babel.min.js";
const babel=require(babelPath);
for(const file of ["vendor/animations-v3.jsx","vendor/tweaks-panel.jsx","motion.jsx"]){
 const source=fs.readFileSync(path.join(__dirname,file),"utf8");
 const code=babel.transform(source,{presets:["react"],sourceType:"script",comments:false,compact:false,filename:file}).code;
 fs.writeFileSync(path.join(__dirname,file.replace(/\.jsx$/,".js")),code+"\n");
}
console.log("Compiled timeline starter, tweaks and four-module animation.");
