GIT_ROOT = `git rev-parse --show-toplevel`.strip

task :default do
  sh "rake -T"
end

desc 'code formatter'
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


desc 'library linux 64'
task :lib_linux_64 do
  puts 'NOTE(pyoung) building on Ubuntu 64bit'
  build_dir = 'build/linux_64'
  lib_dir = 'lib/linux_64'

  sqlcipher_version = 'v3.4.2'

  FileUtils.mkdir_p(build_dir) unless File.directory?(build_dir)
  FileUtils.mkdir_p(lib_dir) unless File.directory?(lib_dir)

  Dir.chdir(build_dir) do
    sh 'sudo apt-get install tcl -y'
    sh 'sudo apt-get install libssl-dev -y'
    sh 'git clone https://github.com/sqlcipher/sqlcipher.git'
    Dir.chdir('sqlcipher') do
      sh "git checkout #{sqlcipher_version}"
      sh './configure -enable-tempstore=no --disable-tcl CFLAGS="-DSQLITE_HAS_CODEC -DSQLCIPHER_CRYPTO_OPENSSL"'
      sh 'make clean'
      sh 'make sqlite3.c'
      sh 'make'
      libsqlcipher_fpath = `readlink -f .libs/libsqlcipher.so`.strip

      # NOTE(pyoung)
      # How find `libcrypto.so`
      # apt-file find libssl-dev | grep libcrypto.so
      libcrypto_fpath = `readlink -f /usr/lib/x86_64-linux-gnu/libcrypto.so`.strip
      puts :libsqlcipher_fpath, libsqlcipher_fpath
      puts :libcrypto_fpath, libcrypto_fpath
      cp libsqlcipher_fpath, File.expand_path(File.join(GIT_ROOT, lib_dir, 'libsqlcipher.so'))
      cp libcrypto_fpath, File.expand_path(File.join(GIT_ROOT, lib_dir, 'libcrypto.so'))
    end
  end
end

desc 'library macos 64'
task :lib_macos_64 do
  puts "NOTE(pyoung) building on macOS 64bit"

  build_dir = 'build/macOS_64'
  lib_dir = 'lib/macOS_64'

  sqlcipher_version = 'v3.4.2'

  FileUtils.mkdir_p(build_dir) unless File.directory?(build_dir)
  FileUtils.mkdir_p(lib_dir) unless File.directory?(lib_dir)

  Dir.chdir(build_dir) do
    sh 'brew install coreutils'
    sh 'brew install tcl-tk'
    sh 'brew install openssl'
    sh 'git clone https://github.com/sqlcipher/sqlcipher.git'
    Dir.chdir('sqlcipher') do
      sh "git checkout #{sqlcipher_version}"
      sh './configure -enable-tempstore=no --disable-tcl CFLAGS="-DSQLITE_HAS_CODEC -DSQLCIPHER_CRYPTO_OPENSSL -I/usr/local/opt/openssl/include/ -L/usr/local/opt/openssl/lib" LDFLAGS="-lcrypto"'
      sh 'make clean'
      sh 'make sqlite3.c'
      sh 'make'
      libsqlcipher_fpath = `greadlink -f .libs/libsqlcipher.dylib`.strip
      libcrypto_fpath = `greadlink -f /usr/lib/libcrypto.dylib`.strip
      puts :libsqlcipher_fpath, libsqlcipher_fpath
      puts :libcrypto_fpath, libcrypto_fpath
      cp libsqlcipher_fpath, File.expand_path(File.join(GIT_ROOT, lib_dir, 'sqlcipher.bundle'))
      cp libcrypto_fpath, File.expand_path(File.join(GIT_ROOT, lib_dir, 'crypto.bundle'))
    end
  end

end
