task :fmt do
  # https://github.com/dotnet/codeformatter
  # codeformatter /rules
  # Name                 Description
  # ==============================================
  # o BraceNewLine         :Ensure all braces occur on a new line
  # x Copyright            :Insert the copyright header into every file
  # x CustomCopyright      :Remove any custom copyright header from the file
  # o NewLineAbove         :Ensure there is a new line above the first namespace and using in the file
  # x UnicodeLiterals      :Use unicode escape sequence instead of unicode literals
  # o UsingLocation        :Place using directives outside namespace declarations
  # x ExplicitThis         :Remove explicit this/Me prefixes on expressions except where necessary
  # o ExplicitVisibility   :Ensure all members have an explicit visibility modifier
  # o FormatDocument       :Run the language specific formatter on every document
  # o IllegalHeaders       :Remove illegal headers from files
  # x ReadonlyFields       :Mark fields which can be readonly as readonly
  # o FieldNames           :Prefix private fields with _ and statics with s_
  Dir.chdir('SqlCipher4Unity3D') do
    enable_rules = ['BraceNewLine', 'UsingLocation', 'FormatDocument', 'NewLineAbove', 'ExplicitVisibility', 'IllegalHeaders', 'FieldNames']
    disable_rules = ['Copyright', 'CustomCopyright', 'UnicodeLiterals', 'ReadonlyFields', 'ExplicitThis']
    sh "codeformatter codeformatter.csproj /rule+:#{enable_rules.join(',')} /rule-:#{disable_rules.join(',')} /verbose"
  end
end
