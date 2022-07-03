supported_rule_kinds = [
  "fsharp_binary",
  "fsharp_library",
  "csharp_binary",
  "csharp_library",
  "import_library",
]

def flatten(xss):
  result = []
  for xs in xss:
    for x in xs:
      result.append(x)
  return result

def structify_file(x):
  return struct(
    is_directory = x.is_directory,
    is_source = x.is_source,
    path = x.path,
    root = x.root.path,
  )

def structify_ref(x):
  return struct(
    files = [ structify_file(f) for f in x.files.to_list() ],
  )

def structify_src(x):
  return struct(
    label = str(x.label),
    files = [ structify_file(f) for f in x.files.to_list() ],
  )

def structify_target(x):
  # print("TARGET")
  # print(dir(x))
  # print(x)
  return struct(
    files = [ structify_file(f) for f in x.files.to_list() ],
  )

def structify_rule(rule):
  if rule.kind in [ "fsharp_binary", "fsharp_library", "csharp_binary", "csharp_library" ]:
    # print(dir(rule.attr))
    # print(rule.attr.testonly)
    return struct(
      name = rule.attr.name,
      kind = rule.kind,
      srcs = [ structify_src(x) for x in rule.attr.srcs ],
      deps = [ str(x.label) for x in rule.attr.deps ],
      testonly = rule.attr.testonly,
    )

  if rule.kind == "import_library":
    for x in rule.attr.libs:
      print("rule.attr.libs")
      print(type(x))
      print(dir(x))
      print(x)
    return struct(
      kind = rule.kind,
      libs = [ structify_target(x) for x in rule.attr.libs ],
      deps = [ str(x.label) for x in rule.attr.deps ],
      refs = [ structify_ref(x) for x in rule.attr.refs ],
    )

  return struct()

def _impl(target, ctx):
  if ctx.rule.kind in supported_rule_kinds:
    # if hasattr(ctx.rule.attr, 'srcs'):

      # print(dir(ctx.rule.attr))
      # print(ctx.rule.attr.deps)

      # if ctx.rule.attr.deps:
      #   for d in ctx.rule.attr.deps:
      #     print(dir(d))
      #     print(dir(d.info))

      # for x in ctx.rule.attr.srcs:
      #   print(x)

      # fsharp_target = Rule(
      #   kind = ctx.rule.kind,
      #   attr = ctx.rule.attr,
      #   # build_file_path = ctx.build_file_path,
      #   # srcs = flatten([ [ f.path for f in x.files.to_list() ] for x in ctx.rule.attr.srcs ]),
      #   # deps = [ str(x.label) for x in ctx.rule.attr.deps ],
      # )

      # print(fsharp_target)

    # print(dir(ctx))
    # print(ctx.rule.attr)

    json_file = ctx.actions.declare_file('bazel2proj_%s.json' % (target.label.name))

    json = struct(
      build_file_path = ctx.build_file_path,
      workspace_name = ctx.workspace_name,
      label = str(ctx.label),
      rule = structify_rule(ctx.rule),
    ).to_json()

    ctx.actions.write(json_file, json)
    # print("WROTE " + json_file.path)

    transitive_jsons = depset([ json_file ])

    if ctx.rule.attr.deps:
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
    # print(dir(ctx.rule.attr))
    # print(ctx.rule.attr)

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

bazel2proj_aspect = aspect(
  implementation = _impl,
  attr_aspects = [ 'deps' ],
)
