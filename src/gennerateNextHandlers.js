// JavaScript source code
// This is a bit crude, but it works for now, can always make it better if we need it again, for now just storing for reference.


for (let i = 0; i < 16; i++) {
    const code = template(i);
    console.log(code);
}

console.log(factoryTemplate(16));

function factoryTemplate(count) {

    let code = `
    public static class NextFactory
    {
    `

    for (let i = 0; i < count; i++) {
        code += method(i);
    }

    code += `
    }
    `
    return code;
    function method(count) {
        function gen(base) {
            return generate(base, count).join(", ");
        }

        return `
        public static INext<${gen("T{0}")}> Create<${gen("T{0}")}>(IPipelineContext context, INode next, ${gen("string param{0}Name")})
            => new Next<${gen("T{0}")}>(context, next, ${gen("param{0}Name")});
        `
    }

}


function template(count) {

    function gen(base) {
        return generate(base, count).join(", ");
    }
    function genl(base) {
        return generate(base, count).join("\n");
    }

    return `
    public interface INext<${gen("in T{0}")}> : INext
    {
        Task<JObject> Invoke(${gen("T{0} arg{0}")});
    }

    public class Next<${gen("T{0}")}> : Next, INext<${gen("T{0}")}>
    {
${genl("        private readonly string arg{0}Name;")}
 
        public Next(IPipelineContext context, INode next, ${gen("string arg{0}Name")})
            : base(context, next)
        {
${genl("            this.arg{0}Name = arg{0}Name;")}
        }

        public Task<JObject> Invoke(${gen("T{0} arg{0}")}) => NextNode.Invoke(Context
            .Replace(${gen("(arg{0}Name, arg{0})")}));
    }`
}


function generate(base, count) {
    const list = [];
    for (let i = 1; i <= count; i++)
        list.push(format(base, i));
    return list;
}

function format(str, ...args) {
    //console.log(args);
    return str.replace(/{\d+}/g, match => {
        //console.log("REPLACE: " + match);
        const str = match.toString();
        const index = parseInt(str.substring(1, str.length - 1));
        //console.log("REPLACE: " + index);
        if (args[index]) {
            return args[index].toString();
        }
        return match;
    })

}