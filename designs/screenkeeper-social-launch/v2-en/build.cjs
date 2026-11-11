const fs=require("node:fs"),path=require("node:path");
let babel;
try{babel=require(process.env.SCREENKEEPER_BABEL||"@babel/standalone");}
catch{console.error("找不到 Babel。请设置 SCREENKEEPER_BABEL 指向本地 Babel standalone 文件，或提供 @babel/standalone 模块。\nBabel is unavailable. Set SCREENKEEPER_BABEL to a local standalone file, or make @babel/standalone resolvable.");process.exit(1);}
for(const file of ["vendor/animations-v3.jsx","vendor/tweaks-panel.jsx","motion.jsx"]){
 const source=fs.readFileSync(path.join(__dirname,file),"utf8");
 const code=babel.transform(source,{presets:["react"],sourceType:"script",comments:false,compact:false,filename:file}).code;
 fs.writeFileSync(path.join(__dirname,file.replace(/\.jsx$/,".js")),code+"\n");
}
console.log("Compiled timeline starter, tweaks and four-module animation.");
