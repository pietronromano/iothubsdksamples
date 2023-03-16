# SOURCE:  GitHub, create a Repository
https://github.com/pietronromano/iothubsdksamples

# Create a new  Git repository on the command line
git init

# In root folder, add a .gitignore
# Exclude any bin or obj directory at any level
**/bin/
**/obj/
# Include .vscode dir
!**/.vscode/**

git add .
git commit -m "First commit"
git remote add origin https://github.com/pietronromano/iothubsdksamples.git
git push -u origin main

# Initialize on another machine
git clone https://github.com/pietronromano/MyIoT.git

## Recover last session from other machines
git pull origin main