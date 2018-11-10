import subprocess
import os
import os.path

compiler_path = "badcc/bin/debug/badcc"

stages_folder = "write_a_c_compiler-master"

glb_good_tests = 0
glb_total_tests = 0

def compile_file(file_name):
    process = subprocess.Popen([compiler_path, file_name])
    process.wait()
    return process.returncode == 0

def run_stage_tests(stage_nr, variant = None):
    print("Running valid tests for stage", stage_nr, variant)
    base_path = os.path.join(stages_folder, "stage_" + str(stage_nr), "valid")
    if variant is not None:
        base_path = os.path.join(base_path, variant)
    
    valid_tests =  os.listdir(base_path)
    good_tests = 0
    total_tests = 0
    for test in valid_tests:
        if test.endswith(".c"):
            total_tests += 1
            test_path = os.path.join(base_path, test)
            if compile_file(test_path):
                print("Success,", test)
                good_tests += 1
            else:
                print("Failed,", test)

    print("Running invalid tests for stage", stage_nr)
    base_path = os.path.join(stages_folder, "stage_" + str(stage_nr), "invalid")
    if variant is not None:
        base_path = os.path.join(base_path, variant)
    
    valid_tests = os.listdir(base_path)
    for test in valid_tests:
        if test.endswith(".c"):
            total_tests += 1
            test_path = os.path.join(base_path, test)
            if not compile_file(test_path):
                print("Success,", test)
                good_tests += 1
            else:
                print("Failed,", test)

    print("Passed", good_tests, "/", total_tests, "tests for stage", stage_nr, variant, "\n")

    global glb_good_tests
    global glb_total_tests

    glb_good_tests += good_tests
    glb_total_tests += total_tests

#run_stage_tests(1)
#run_stage_tests(2)
#run_stage_tests(3)
#run_stage_tests(4)
#run_stage_tests(5)
#run_stage_tests(6, "expression")
#run_stage_tests(6, "statement")
#run_stage_tests(7)
run_stage_tests(9)

if glb_good_tests == glb_total_tests:
    print("PASSED:", glb_good_tests, "/", glb_total_tests, "tests succeeded")
else:
    print("FAILED:", glb_good_tests, "/", glb_total_tests, "tests succeeded")
