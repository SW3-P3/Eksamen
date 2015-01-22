for file in ./*.cs
do
    if [ -a $file ]
    then
        tail -c +4 $file > $file.truncated && mv $file.truncated $file
    fi
done
