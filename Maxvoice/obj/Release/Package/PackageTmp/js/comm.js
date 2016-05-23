function parseAspDtTm(val) {
    var re = /-?\d+/;
    var m = re.exec(val);
    return new Date(parseInt(m[0]));
}

Date.prototype.format = function (format) 
{
    var o = {
        "M+": this.getMonth() + 1,
        "d+": this.getDate(),
        "h+": this.getHours(),
        "m+": this.getMinutes(),
        "s+": this.getSeconds(),
        "q+": Math.floor((this.getMonth() + 3) / 3),
        "S": this.getMilliseconds()
    }
    if (/(y+)/.test(format)) format = format.replace(RegExp.$1,
    (this.getFullYear() + "").substr(4 - RegExp.$1.length));
    for (var k in o) if (new RegExp("(" + k + ")").test(format))
        format = format.replace(RegExp.$1,
      RegExp.$1.length == 1 ? o[k] :
        ("00" + o[k]).substr(("" + o[k]).length));
    return format;
}

String.prototype.remove = function (reg) {
    var s = "";
    var p = new RegExp(reg);
    for (var i = 0; i < this.length; i++) {
        if (p.test(this.charAt(i))) continue;
        s += this.charAt(i);
    }
    return s;
}

function nvl(v) {
    return v ? v : "";
}