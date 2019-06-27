require 'mkmf'
GIT_ROOT = `git rev-parse --show-toplevel`.strip if find_executable 'git'
RAKE_ROOT = Rake.application.original_dir

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

desc 'library windows 64'
task :lib_win_64 do
  # [normal terminal]
  # scoop install ruby msys2
  # [mingw64 terminal]
  # export PATH=$PATH:/c/Users/netpyoung/scoop/apps/ruby/current/gems/bin/:/c/Users/netpyoung/scoop/apps/ruby/current/bin/

  # https://slproweb.com/products/Win32OpenSSL.html
  # Win64 OpenSSL v1.0.2q # C:\OpenSSL-Win64 - check The OpenSSL binaries (/bin) directory
  # Win32 OpenSSL v1.0.2q # C:\OpenSSL-Win32 - check The OpenSSL binaries (/bin) directory

  build_dir = 'build/win_64'
  lib_dir = 'lib/win_64'

  sqlcipher_version = 'v3.4.2'

  FileUtils.mkdir_p(build_dir) unless File.directory?(build_dir)
  FileUtils.mkdir_p(lib_dir) unless File.directory?(lib_dir)
  dst_libsqlcipher = File.join(`pwd`.strip, lib_dir, 'sqlcipher.dll')
  dst_libcrypto = File.join(`pwd`.strip, lib_dir, 'libeay32.dll')

  Dir.chdir(build_dir) do
    sh 'pacman --noconfirm --needed -Syu'
    sh 'pacman --noconfirm --needed -S git'
    sh 'pacman --noconfirm --needed -S base-devel'
    sh 'pacman --noconfirm --needed -S mingw32/mingw-w64-i686-gcc' # 32-bit
    sh 'pacman --noconfirm --needed -S mingw64/mingw-w64-x86_64-gcc' # 64-bit
    sh 'pacman --noconfirm --needed -S tcl'

    sh 'git clone https://github.com/sqlcipher/sqlcipher.git'
    Dir.chdir('sqlcipher') do
      sh "git checkout #{sqlcipher_version}"
      pwd = `pwd`.strip
      puts pwd

      open_ssl_dir = '/c/OpenSSL-Win64'
      libcrypto_fpath = "#{open_ssl_dir}/bin/libeay32.dll"
      sh "cp #{libcrypto_fpath} ./"

      sh %Q[sh ./configure --with-crypto-lib=none --disable-tcl CFLAGS="-DSQLITE_HAS_CODEC -DSQLCIPHER_CRYPTO_OPENSSL -I#{open_ssl_dir}/include #{open_ssl_dir}/bin/libeay32.dll -L#{pwd} -static-libgcc" LDFLAGS="-leay32"]
      sh 'make clean'
      sh 'make sqlite3.c'
      sh 'make'
      sh 'make dll'
      libsqlcipher_fpath = '.libs/libsqlcipher-0.dll'
      puts :libsqlcipher_fpath, libsqlcipher_fpath
      puts :libcrypto_fpath, libcrypto_fpath
      sh "cp #{libsqlcipher_fpath} #{dst_libsqlcipher}"
      sh "cp #{libcrypto_fpath} #{dst_libcrypto}"
    end
  end
end

desc 'library android'
task :lib_android do
  lib_dir = 'lib/android'

  sqlcipher_version = '3.5.9'
  FileUtils.mkdir_p(lib_dir) unless File.directory?(lib_dir)

  Dir.chdir(lib_dir) do
    sh "mvn org.apache.maven.plugins:maven-dependency-plugin:2.8:get -DremoteRepositories=https://repo.maven.apache.org/maven2 -Dartifact=net.zetetic:android-database-sqlcipher:#{sqlcipher_version}:aar -Ddest=android-database-sqlcipher-#{sqlcipher_version}.aar"
  end
end
