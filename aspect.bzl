FSharpTarget = provider(
  fields = {
    'kind' : 'Kind of the target',
    'build_file_path' : 'path to BUILD file',
    'srcs' : 'list of source files',
    'deps' : 'list of dependencies',
  }
)

supported_rule_kinds = [
  "fsharp_binary",
  "fsharp_library",
]

def flatten(xss):
  result = []
  for xs in xss:
    for x in xs:
      result.append(x)
  return result

def _print_aspect_impl(target, ctx):
  if ctx.rule.kind in supported_rule_kinds:
    if hasattr(ctx.rule.attr, 'srcs'):

      # print(dir(ctx.rule.attr))
      # print(ctx.rule.attr.deps)

      if ctx.rule.attr.deps:
        for d in ctx.rule.attr.deps:
          print(dir(d))
          print(dir(d.info))

      # for x in ctx.rule.attr.srcs:
      #   print(x)

      fsharp_target = FSharpTarget(
        kind = ctx.rule.kind,
        build_file_path = ctx.build_file_path,
        srcs = flatten([ [ f.path for f in x.files.to_list() ] for x in ctx.rule.attr.srcs ]),
        deps = [ str(x.label) for x in ctx.rule.attr.deps ],
      )

      # print(fsharp_target)

      json_file = ctx.actions.declare_file('bazel2proj_%s.json' % (target.label.name))

      ctx.actions.write(json_file, fsharp_target.to_json())
      # print("WROTE " + json_file.path)

      transitive_jsons = depset([ json_file ])

      for dep in ctx.rule.attr.deps:
        info = dep.info
        transitive_jsons = depset(transitive=[info.transitive_jsons, transitive_jsons])

      return struct(
        info = struct(
          jsons = [ json_file ],
          transitive_jsons = transitive_jsons,
        ),
        output_groups = {
          "jsons": transitive_jsons,
        },
      )

  else:
    print("Unsupported rule kind: " + ctx.rule.kind)

  return struct(
    info = struct(
      jsons = [],
      transitive_jsons = depset([]),
    ),
    output_groups = {
      "jsons": [],
    },
  )

  # print(ctx.rule.kind)
  # # Make sure the rule has a srcs attribute.
  # if hasattr(ctx.rule.attr, 'srcs'):
  #   # Iterate through the files that make up the sources and
  #   # print their paths.
  #   for src in ctx.rule.attr.srcs:
  #     for f in src.files.to_list():
  #       print(f.path)
  # return []

print_aspect = aspect(
  implementation = _print_aspect_impl,
  attr_aspects = [ 'deps' ],
)
